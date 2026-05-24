using System.Collections.Generic;

namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class ShopRuntimeState
    {
        public int SkillMerchantRefreshIndex;
        public List<string> SkillMerchantStockSkillIds = new List<string>();

        public void Reset()
        {
            SkillMerchantRefreshIndex = 0;
            if (SkillMerchantStockSkillIds == null)
            {
                SkillMerchantStockSkillIds = new List<string>();
            }

            SkillMerchantStockSkillIds.Clear();
        }
    }
}
