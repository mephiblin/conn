using Conn.Runtime.Session;
using Conn.Runtime.Content;
using Conn.Runtime.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Conn.UI.Runtime
{
    public sealed class SkillFaceDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static string draggedSkillId = string.Empty;
        private static GameObject dragGhost;

        [SerializeField] private string skillId = string.Empty;
        [SerializeField] private int dieIndex = -1;
        [SerializeField] private int faceIndex = -1;
        [SerializeField] private bool dragSource;
        [SerializeField] private bool dropTarget;
        private CanvasGroup sourceCanvasGroup;
        private float sourceOriginalAlpha = 1f;
        private bool sourceDragActive;

        public void ConfigureDragSource(string sourceSkillId)
        {
            skillId = sourceSkillId;
            faceIndex = -1;
            dragSource = true;
            dropTarget = false;
        }

        public void ConfigureDropTarget(int targetFaceIndex)
        {
            ConfigureDropTarget(0, targetFaceIndex);
        }

        public void ConfigureDropTarget(int targetDieIndex, int targetFaceIndex)
        {
            skillId = string.Empty;
            dieIndex = targetDieIndex;
            faceIndex = targetFaceIndex;
            dragSource = false;
            dropTarget = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragSource)
            {
                draggedSkillId = skillId;
                sourceCanvasGroup = GetComponent<CanvasGroup>();
                if (sourceCanvasGroup == null)
                {
                    sourceCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                sourceOriginalAlpha = sourceCanvasGroup.alpha;
                sourceCanvasGroup.alpha = 0.45f;
                sourceDragActive = true;
                CreateDragGhost(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveDragGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            draggedSkillId = string.Empty;
            if (sourceCanvasGroup != null)
            {
                sourceCanvasGroup.alpha = sourceOriginalAlpha;
            }

            sourceDragActive = false;
            DestroyDragGhost();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!dropTarget || string.IsNullOrWhiteSpace(draggedSkillId) || GameSession.Instance == null)
            {
                return;
            }

            SkillRuntimeService.EquipSkillToDieFace(GameSession.Instance.State, draggedSkillId, dieIndex, faceIndex);
            draggedSkillId = string.Empty;
            DestroyDragGhost();
        }

        private void OnDisable()
        {
            if (!sourceDragActive)
            {
                return;
            }

            draggedSkillId = string.Empty;
            if (sourceCanvasGroup != null)
            {
                sourceCanvasGroup.alpha = sourceOriginalAlpha;
            }

            sourceDragActive = false;
            DestroyDragGhost();
        }

        private void CreateDragGhost(PointerEventData eventData)
        {
            DestroyDragGhost();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var skill = RuntimeContentDatabase.FindSkill(skillId);
            dragGhost = new GameObject("SkillDragGhost");
            dragGhost.transform.SetParent(canvas.transform, false);
            dragGhost.transform.SetAsLastSibling();

            var rect = dragGhost.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(168f, 74f);

            var image = dragGhost.AddComponent<Image>();
            image.color = new Color(0.15f, 0.20f, 0.28f, 0.90f);
            image.raycastTarget = false;

            var group = dragGhost.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 8, 8);
            group.spacing = 2f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = false;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            AddGhostText(skill != null ? skill.DisplayName : skillId, 14, FontStyle.Bold);
            AddGhostText(skill != null ? $"{skill.EffectKind} +{skill.Power}" : "Skill", 11, FontStyle.Normal);
            MoveDragGhost(eventData);
        }

        private static void AddGhostText(string value, int size, FontStyle style)
        {
            if (dragGhost == null)
            {
                return;
            }

            var obj = new GameObject("Text");
            obj.transform.SetParent(dragGhost.transform, false);
            var text = obj.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(18f, size + 6f);
        }

        private static void MoveDragGhost(PointerEventData eventData)
        {
            var rect = dragGhost != null ? dragGhost.GetComponent<RectTransform>() : null;
            if (rect != null && eventData != null)
            {
                rect.position = eventData.position + new Vector2(18f, -18f);
            }
        }

        private static void DestroyDragGhost()
        {
            if (dragGhost == null)
            {
                return;
            }

            Destroy(dragGhost);
            dragGhost = null;
        }
    }
}
