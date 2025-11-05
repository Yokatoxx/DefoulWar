namespace FPS
{
    /// <summary>
    /// Multiplicateur de dégâts par zone pour les armes.
    /// </summary>
    [System.Serializable]
    public struct HitZoneMultiplier
    {
        /// <summary>
        /// Nom de la zone (ex: Body, Head).
        /// </summary>
        public string zoneName;
        /// <summary>
        /// Multiplicateur appliqué aux dégâts de base (>= 0).
        /// </summary>
        public float multiplier;

        /// <summary>
        /// Crée un multiplicateur de dégâts pour une zone.
        /// </summary>
        public HitZoneMultiplier(string name, float mult)
        {
            zoneName = name;
            multiplier = mult;
        }
    }
}
