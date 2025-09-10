using UnityEngine;
using PlayMySpace.PMSC.Events;

public class DevilProjectileAttackScript : MonoBehaviour
{
    [SerializeField] private GameObject devilProjectilePrefab;
    [SerializeField] private GameObject devilProjectileSpawnLocation;
    [SerializeField] private PMEDevilBehaviorScript devil;
    
    public void DevilProjectileAttack()
    {
        GameObject devilProjectile = Instantiate(devilProjectilePrefab, devilProjectileSpawnLocation.transform.position, transform.rotation, null);
        devilProjectile.transform.rotation = Quaternion.LookRotation(devil.TargetCharacter.transform.position - devilProjectile.transform.position, transform.up) * Quaternion.AngleAxis(180, transform.up);
        devilProjectile.GetComponent<DevilProjectileScript>().ExecuteBehavior(devil.TargetCharacter);
    }
}
