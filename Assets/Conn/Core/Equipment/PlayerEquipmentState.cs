namespace Conn.Core.Equipment
{
    [System.Serializable]
    public sealed class PlayerEquipmentState
    {
        public WeaponGrip WeaponGrip = WeaponGrip.OneHand;

        public int DiceCount
        {
            get
            {
                return WeaponGrip switch
                {
                    WeaponGrip.OneHand => 4,
                    WeaponGrip.OneHandAndShield => 3,
                    WeaponGrip.TwoHand => 5,
                    _ => 2
                };
            }
        }

        public int DefenseBonus => WeaponGrip == WeaponGrip.OneHandAndShield ? 1 : 0;
    }
}
