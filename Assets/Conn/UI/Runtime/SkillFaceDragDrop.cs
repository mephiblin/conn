using Conn.Runtime.Session;
using Conn.Runtime.Skills;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Conn.UI.Runtime
{
    public sealed class SkillFaceDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static string draggedSkillId = string.Empty;

        [SerializeField] private string skillId = string.Empty;
        [SerializeField] private int faceIndex = -1;
        [SerializeField] private bool dragSource;
        [SerializeField] private bool dropTarget;

        public void ConfigureDragSource(string sourceSkillId)
        {
            skillId = sourceSkillId;
            faceIndex = -1;
            dragSource = true;
            dropTarget = false;
        }

        public void ConfigureDropTarget(int targetFaceIndex)
        {
            skillId = string.Empty;
            faceIndex = targetFaceIndex;
            dragSource = false;
            dropTarget = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragSource)
            {
                draggedSkillId = skillId;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            draggedSkillId = string.Empty;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!dropTarget || string.IsNullOrWhiteSpace(draggedSkillId))
            {
                return;
            }

            SkillRuntimeService.EquipSkillToFace(GameSession.Instance.State, draggedSkillId, faceIndex);
            draggedSkillId = string.Empty;
        }
    }
}
