// filepath: e:\Documents\Projet Unity\DefoulWar\Assets\Scripts\FPS\IDamageInterceptor.cs
namespace FPS
{
    // Permet à un composant sur l'ennemi de modifier/bloquer le DamageInfo avant application
    public interface IDamageInterceptor
    {
        // Retourne true si le dégât doit être appliqué, false pour le bloquer
        bool OnBeforeDamage(ref DamageInfo damage);
    }
}

