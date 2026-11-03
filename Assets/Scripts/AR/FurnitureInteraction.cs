using System;
using System.Collections.Generic;
using ARProductViewer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Turns touch (and, in the Editor, mouse) input into furniture actions:
    /// tap a placed piece to select or deselect it, tap empty floor to place a new piece at the reticle,
    /// drag the selected piece along the floor, and twist or pinch with two fingers to rotate or scale it.
    /// Gesture maths and selection state live in Core; this component only reads input and applies results.
    /// </summary>
    [DefaultExecutionOrder(10)] // After ARTapToPlace, so a tap places at the pose the reticle shows this frame.
    public sealed class FurnitureInteraction : MonoBehaviour
    {
        enum GestureState
        {
            Idle,
            Pressing,  // One finger down, not yet beyond the drag threshold: may still become a tap.
            Dragging,  // Moving the selected piece along the floor.
            TwoFinger, // Rotating / scaling the selected piece.
            Consumed,  // Gesture is not a tap (over UI, swipe, leftover finger); ignore until all fingers lift.
        }

        static readonly List<RaycastResult> s_UIHits = new List<RaycastResult>();

        [SerializeField]
        ARTapToPlace m_Placer;

        [SerializeField, Tooltip("AR camera. Defaults to Camera.main.")]
        Camera m_Camera;

        [SerializeField, Tooltip("Ring shown under the selected piece.")]
        SelectionIndicator m_IndicatorPrefab;

        [SerializeField, Tooltip("Physics layer placed furniture is put on, so taps only hit furniture.")]
        string m_FurnitureLayer = "PlacedFurniture";

        [SerializeField, Tooltip("Finger travel, in density-independent pixels, before a press becomes a drag.")]
        float m_DragThresholdDp = 10f;

        [SerializeField, Tooltip("Maximum distance, in metres, at which a tap can hit placed furniture.")]
        float m_MaxTapDistance = 20f;

        readonly Dictionary<int, AnchoredProduct> m_Items = new Dictionary<int, AnchoredProduct>();
        readonly FloorDrag m_Drag = new FloorDrag();

        PlacedItemSelection m_Selection;
        SelectionIndicator m_Indicator;
        int m_LayerIndex;
        int m_LayerMask;

        GestureState m_State;
        Float2 m_PressStart;
        AnchoredProduct m_PressedItem;
        float m_GrabHeight;
        AnchoredProduct m_ManipulatedItem;
        Float2 m_PreviousA;
        Float2 m_PreviousB;

        /// <summary>Selection shared with the UI. Created on demand if none is injected.</summary>
        public PlacedItemSelection Selection => m_Selection ??= CreateSelection();

        public int PlacedCount => m_Items.Count;

        /// <summary>Raised after a new piece has been placed.</summary>
        public event Action<AnchoredProduct> Placed;

        /// <summary>Uses <paramref name="selection"/> as the shared selection state. Call before the first frame.</summary>
        public void Initialize(PlacedItemSelection selection)
        {
            if (m_Selection != null)
                m_Selection.SelectionChanged -= OnSelectionChanged;

            m_Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            m_Selection.SelectionChanged += OnSelectionChanged;
            OnSelectionChanged(m_Selection.SelectedId);
        }

        /// <summary>Removes the selected piece and releases its anchor.</summary>
        public void DeleteSelected()
        {
            if (!Selection.SelectedId.HasValue || !m_Items.TryGetValue(Selection.SelectedId.Value, out var item))
                return;

            CancelGesture();
            m_Items.Remove(item.Id);
            Selection.NotifyRemoved(item.Id);
            Destroy(item.gameObject); // AnchoredProduct releases its anchor in OnDestroy.
        }

        void Awake()
        {
            if (m_Camera == null)
                m_Camera = Camera.main;

            m_LayerIndex = LayerMask.NameToLayer(m_FurnitureLayer);
            if (m_LayerIndex < 0)
            {
                Debug.LogError($"{nameof(FurnitureInteraction)}: layer '{m_FurnitureLayer}' is missing; taps cannot select furniture.", this);
                m_LayerIndex = 0;
            }
            m_LayerMask = 1 << m_LayerIndex;

            if (m_IndicatorPrefab != null)
            {
                m_Indicator = Instantiate(m_IndicatorPrefab);
                m_Indicator.Follow(null);
            }
        }

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDisable()
        {
            CancelGesture();
            EnhancedTouchSupport.Disable();
        }

        void OnDestroy()
        {
            if (m_Selection != null)
                m_Selection.SelectionChanged -= OnSelectionChanged;
            if (m_Indicator != null)
                Destroy(m_Indicator.gameObject);
        }

        void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count > 0)
                HandleTouches(touches);
            else
                HandleMouse();

            if (m_Placer != null)
                m_Placer.SuppressPreview = IsEditingPlacedItem;
        }

        /// <summary>A placed piece is selected or being manipulated: the placement preview stays hidden.</summary>
        bool IsEditingPlacedItem =>
            Selection.HasSelection || m_State == GestureState.Dragging || m_State == GestureState.TwoFinger;

        // ---- Input routing ------------------------------------------------------------------------

        void HandleTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
        {
            if (touches.Count >= 2)
            {
                var a = ToFloat2(touches[0].screenPosition);
                var b = ToFloat2(touches[1].screenPosition);
                if (m_State != GestureState.TwoFinger && m_State != GestureState.Consumed)
                    BeginTwoFinger(a, b);
                else if (m_State == GestureState.TwoFinger)
                    UpdateTwoFinger(a, b);
                return;
            }

            var touch = touches[0];
            var position = ToFloat2(touch.screenPosition);

            if (m_State == GestureState.TwoFinger)
            {
                // A finger lifted: finish rotating/scaling and ignore the remaining finger.
                EndManipulation();
                m_State = GestureState.Consumed;
            }

            if (touch.began && m_State == GestureState.Idle)
                BeginPress(position);
            else if (m_State == GestureState.Pressing || m_State == GestureState.Dragging)
                UpdatePress(position);

            if (touch.ended)
                EndPress(position);
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                // No touches and no mouse: make sure nothing is left half-finished.
                if (m_State != GestureState.Idle)
                    CancelGesture();
                return;
            }

            var position = ToFloat2(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame)
                BeginPress(position);
            else if (mouse.leftButton.isPressed)
                UpdatePress(position);

            if (mouse.leftButton.wasReleasedThisFrame)
                EndPress(position);
            else if (!mouse.leftButton.isPressed && m_State != GestureState.Idle)
                CancelGesture();
        }

        // ---- One finger: tap or drag --------------------------------------------------------------

        void BeginPress(Float2 position)
        {
            m_PressStart = position;
            if (IsOverUI(position))
            {
                m_State = GestureState.Consumed;
                return;
            }

            m_PressedItem = RaycastFurniture(position, out m_GrabHeight);
            m_State = GestureState.Pressing;
        }

        void UpdatePress(Float2 position)
        {
            if (m_State == GestureState.Pressing && ManipulationMath.ExceedsDragThreshold(m_PressStart, position, DragThresholdPixels()))
            {
                var selected = SelectedItem();
                // Drag on a horizontal plane at the height the finger touched the piece, starting from where
                // the finger first went down: the touched spot stays under the finger 1:1, while FloorDrag
                // keeps the piece itself on the floor.
                if (selected != null && m_PressedItem == selected && TryFingerPointAtHeight(m_PressStart, m_GrabHeight, out var grabPoint))
                {
                    m_Drag.Begin(selected.CurrentPose.Position, grabPoint);
                    BeginManipulation(selected);
                    m_State = GestureState.Dragging;
                }
                else
                {
                    // A swipe that did not start on the selected piece: not a tap, not a move.
                    m_State = GestureState.Consumed;
                }
            }

            if (m_State == GestureState.Dragging && m_ManipulatedItem != null
                && TryFingerPointAtHeight(position, m_GrabHeight, out var point))
            {
                var pose = m_ManipulatedItem.CurrentPose;
                m_ManipulatedItem.SetPose(new PlacementPose(m_Drag.Update(point), pose.YawDegrees));
            }
        }

        void EndPress(Float2 position)
        {
            switch (m_State)
            {
                case GestureState.Pressing:
                    HandleTap();
                    break;
                case GestureState.Dragging:
                    m_Drag.End();
                    EndManipulation();
                    break;
            }

            m_State = GestureState.Idle;
            m_PressedItem = null;
        }

        void HandleTap()
        {
            if (m_PressedItem != null)
            {
                Selection.ToggleSelect(m_PressedItem.Id);
                return;
            }

            // Only place where the user can see the ghost: not in view mode and not while editing a placed piece
            // (then a floor tap just deselects, which brings the preview back).
            if (m_Placer != null && PlacementPreviewRules.ShouldPlaceOnFloorTap(m_Placer.IsPlacementActive, m_Placer.HasPlacementPose, Selection.HasSelection))
            {
                var placed = m_Placer.PlaceAtReticle();
                if (placed != null)
                {
                    Register(placed);
                    Selection.Select(placed.Id);
                    Placed?.Invoke(placed);
                }
                return;
            }

            Selection.Deselect();
        }

        // ---- Two fingers: twist and pinch ---------------------------------------------------------

        void BeginTwoFinger(Float2 a, Float2 b)
        {
            if (m_State == GestureState.Dragging)
                m_Drag.End();

            var selected = SelectedItem();
            if (selected == null || IsOverUI(a) || IsOverUI(b))
            {
                EndManipulation();
                m_State = GestureState.Consumed;
                return;
            }

            if (m_ManipulatedItem != selected)
                BeginManipulation(selected);

            m_PreviousA = a;
            m_PreviousB = b;
            m_State = GestureState.TwoFinger;
        }

        void UpdateTwoFinger(Float2 a, Float2 b)
        {
            if (m_ManipulatedItem == null)
            {
                m_State = GestureState.Consumed;
                return;
            }

            var twist = ManipulationMath.TwistDeltaDegrees(m_PreviousA, m_PreviousB, a, b);
            var ratio = ManipulationMath.PinchRatio(m_PreviousA, m_PreviousB, a, b);

            var pose = m_ManipulatedItem.CurrentPose;
            m_ManipulatedItem.SetPose(new PlacementPose(pose.Position, ManipulationMath.ApplyTwist(pose.YawDegrees, twist)));
            m_ManipulatedItem.Scale = ManipulationMath.ApplyPinch(m_ManipulatedItem.Scale, ratio);

            m_PreviousA = a;
            m_PreviousB = b;
        }

        // ---- Helpers ------------------------------------------------------------------------------

        void BeginManipulation(AnchoredProduct item)
        {
            EndManipulation();
            m_ManipulatedItem = item;
            item.BeginManipulation();
        }

        void EndManipulation()
        {
            if (m_ManipulatedItem != null)
                m_ManipulatedItem.EndManipulation();
            m_ManipulatedItem = null;
        }

        void CancelGesture()
        {
            m_Drag.End();
            EndManipulation();
            m_State = GestureState.Idle;
            m_PressedItem = null;
            if (m_Placer != null)
                m_Placer.SuppressPreview = IsEditingPlacedItem;
        }

        void Register(AnchoredProduct item)
        {
            SetLayerRecursively(item.gameObject, m_LayerIndex);
            m_Items[item.Id] = item;
        }

        AnchoredProduct SelectedItem()
        {
            if (!Selection.SelectedId.HasValue)
                return null;

            if (m_Items.TryGetValue(Selection.SelectedId.Value, out var item) && item != null)
                return item;

            // The selected piece no longer exists.
            Selection.Deselect();
            return null;
        }

        AnchoredProduct RaycastFurniture(Float2 screenPosition, out float hitHeight)
        {
            hitHeight = 0f;
            var ray = m_Camera.ScreenPointToRay(new Vector3(screenPosition.X, screenPosition.Y, 0f));
            if (!Physics.Raycast(ray, out var hit, m_MaxTapDistance, m_LayerMask, QueryTriggerInteraction.Ignore))
                return null;

            var item = hit.collider.GetComponentInParent<AnchoredProduct>();
            if (item == null || !m_Items.ContainsKey(item.Id))
                return null;

            hitHeight = hit.point.y;
            return item;
        }

        /// <summary>Where the finger's ray meets the horizontal plane at <paramref name="height"/>.</summary>
        bool TryFingerPointAtHeight(Float2 screenPosition, float height, out Float3 point)
        {
            var ray = m_Camera.ScreenPointToRay(new Vector3(screenPosition.X, screenPosition.Y, 0f));
            return ManipulationMath.TryIntersectFloor(ray.origin.ToFloat3(), ray.direction.ToFloat3(), height, out point);
        }

        float DragThresholdPixels()
        {
            var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return m_DragThresholdDp * dpi / 160f;
        }

        void OnSelectionChanged(int? selectedId)
        {
            if (m_Indicator == null)
                return;

            AnchoredProduct item = null;
            if (selectedId.HasValue)
                m_Items.TryGetValue(selectedId.Value, out item);
            m_Indicator.Follow(item != null ? item.transform : null);
        }

        PlacedItemSelection CreateSelection()
        {
            var selection = new PlacedItemSelection();
            selection.SelectionChanged += OnSelectionChanged;
            return selection;
        }

        static bool IsOverUI(Float2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointerData = new PointerEventData(eventSystem) { position = new Vector2(screenPosition.X, screenPosition.Y) };
            eventSystem.RaycastAll(pointerData, s_UIHits);
            var isOverUI = s_UIHits.Count > 0;
            s_UIHits.Clear();
            return isOverUI;
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        static Float2 ToFloat2(Vector2 v) => new Float2(v.x, v.y);
    }
}
