using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WeaponController : NetworkBehaviour
{
    [Header("HUD Prefab")]
    public GameObject ammoUIPrefabRoot;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    [Header("References")]
    public Transform firePoint; // ✅ Manually assigned in Unity
    public Transform aimTarget; // ✅ Manually assigned in Unity
    public GameObject projectilePrefab; // Assign this in Unity
    public float scatterAngle;
    public string WeaponControllerID;
    private ItemSO currentWeapon;
    private bool isReloading = false;
    private float chargeStartTime;
    private bool isCharging;
    private float maxChargeTime = 5f;
    private float lastFireTime;
    private int currentAmmo; // ✅ Tracks remaining ammo without modifying ItemSO
    private FireMode fireMode = FireMode.Tap; // Default mode
    private Coroutine burstFireRoutine;
    public bool isCombatMode = false;
    private Coroutine ammoRegenRoutine;
    private Dictionary<string, int> weaponAmmo = new();

    public enum FireMode
    {
        Tap,
        Burst,
        Auto,
        ChargedShot
    }
    private IEnumerator AmmoRegeneration()
    {
        while (currentWeapon != null && currentAmmo < currentWeapon.magSize)
        {
            yield return new WaitForSeconds(currentWeapon.reloadSpeed);
            currentAmmo++;
            UpdateAmmoUI();
        }

        ammoRegenRoutine = null;
    }


    private void Start()
    {
        UpdateWeapon(); // ✅ Still call UpdateWeapon here
    }
    public override void OnStartLocalPlayer()
    {
        ammoUIPrefabRoot = GameObject.Find("PlayerHUD"); // ✅ Replace with correct reference if needed
        StartCoroutine(WaitForAmmoText());
    }
    private IEnumerator WaitForAmmoText()
    {
        while (ammoText == null)
        {
            if (ammoUIPrefabRoot != null && ammoUIPrefabRoot.TryGetComponent(out HUDInitializer hud))
            {
                ammoText = hud.ammoText;
                if (ammoText != null)
                {
                    ammoText.gameObject.SetActive(false);
                    UpdateAmmoUI();
                    break;
                }
            }
            yield return null;
        }
    }

    public void ForceUpdateAmmoUI()
    {
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null && currentWeapon != null)
        {
            string modeLabel = fireMode switch
            {
                FireMode.Tap => "Tap",
                FireMode.Burst => "Burst",
                FireMode.Auto => "Auto",
                _ => "N/A"
            };

            ammoText.text = $"{modeLabel}  {currentAmmo} / {currentWeapon.magSize}";

        }
    }
    private void Update()
    {
        if (!isLocalPlayer || currentWeapon == null) return;

        if (!isCombatMode) return; // ✅ Block input unless in combat mode

        HandleFireInput();
    }


    private void HandleFireInput()
    {
        switch (fireMode)
        {
            case FireMode.Tap:
                if (Input.GetButtonDown("Fire1"))
                    FireWeapon();
                break;

            case FireMode.Burst:
                if (Input.GetButtonDown("Fire1") && burstFireRoutine == null)
                {
                    float timeSinceLastBurst = Time.time - lastFireTime;
                    if (timeSinceLastBurst >= (1f / currentWeapon.attackSpeed))
                    {
                        burstFireRoutine = StartCoroutine(BurstFireRoutine());
                    }
                }
                break;


            case FireMode.Auto:
                if (Input.GetButton("Fire1"))
                {
                    float timeSinceLastShot = Time.time - lastFireTime;
                    if (timeSinceLastShot >= (1f / currentWeapon.attackSpeed))
                    {
                        FireWeapon();
                    }
                }
                break;

            case FireMode.ChargedShot:
                if (Input.GetButtonDown("Fire1"))
                {
                    isCharging = true;
                    chargeStartTime = Time.time;
                }

                if (Input.GetButtonUp("Fire1") && isCharging)
                {
                    float chargeDuration = Mathf.Clamp(Time.time - chargeStartTime, 0, maxChargeTime);
                    FireChargedShot(chargeDuration);
                    isCharging = false;
                }
                break;
        }
    }


    private IEnumerator BurstFireRoutine()
    {
        int shotsFired = 0;
        int burstCount = 3;

        while (shotsFired < burstCount)
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("❌ Burst stopped early: no ammo.");
                break;
            }

            FireWeapon();
            shotsFired++;
            yield return new WaitForSeconds(1f / currentWeapon.attackSpeed);
        }

        // Add cooldown after burst is complete
        lastFireTime = Time.time;
        yield return new WaitForSeconds(1f / currentWeapon.attackSpeed); // Delay after entire burst

        burstFireRoutine = null;
    }

    public void UpdateWeapon()
    {
        var tracker = GetComponent<PlayerEquipmentTracker>();
        if (tracker == null) return;
        // Store old ammo if weapon is changing
        if (!string.IsNullOrEmpty(WeaponControllerID) && currentWeapon != null)
            weaponAmmo[WeaponControllerID] = currentAmmo;

        string newID = tracker.GetEquippedWeaponID();
        WeaponControllerID = newID;


        Debug.Log($"🔄 Updating WeaponController. New WeaponControllerID: {WeaponControllerID}");

        if (string.IsNullOrEmpty(WeaponControllerID))
        {
            if (ammoRegenRoutine != null)
            {
                StopCoroutine(ammoRegenRoutine);
                ammoRegenRoutine = null;
            }

            currentWeapon = null;
            currentAmmo = 0;

            if (ammoText != null)
            {
                ammoText.text = "";
                ammoText.gameObject.SetActive(false); // ❌ Hide when no weapon
            }

            Debug.Log("❌ Weapon unequipped. Resetting weapon data.");
            return;
        }

        currentWeapon = ItemDatabaseSO.Instance.GetItemById(WeaponControllerID);
        if (currentWeapon == null)
        {
            Debug.LogError($"❌ Item ID {WeaponControllerID} not found in database.");
            return;
        }

        if (!weaponAmmo.TryGetValue(WeaponControllerID, out currentAmmo))
        {
            currentAmmo = currentWeapon.magSize;
            weaponAmmo[WeaponControllerID] = currentAmmo; // Store initial value
        }

        fireMode = (FireMode)currentWeapon.fireMode;

        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(true);
            UpdateAmmoUI();
        }

        if (currentAmmo < currentWeapon.magSize && ammoRegenRoutine == null)
        {
            ammoRegenRoutine = StartCoroutine(AmmoRegeneration());
        }

        Debug.Log($"✅ Weapon updated: {currentWeapon.itemName}, Ammo: {currentAmmo}");
    }

    private void FireChargedShot(float chargeTime)
    {
        if (currentAmmo <= 0 || isReloading || gameObject.CompareTag("Dead")) return;

        Debug.Log($"🔋 Charged shot fired with {chargeTime:F2}s charge!");

        currentAmmo--;
        if (ammoRegenRoutine == null)
            ammoRegenRoutine = StartCoroutine(AmmoRegeneration());
        UpdateAmmoUI();

        float chargeRatio = chargeTime / maxChargeTime;
        Vector3 shootDir = GetShootDirectionWithScatter();

        CmdShootCharged(firePoint.position, shootDir, chargeRatio);
    }
    [Command]
    private void CmdShootCharged(Vector3 position, Vector3 direction, float chargeRatio)
    {
        if (projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile, gameObject.scene);
        NetworkServer.Spawn(projectile);

        if (projectile.TryGetComponent(out Projectile projectileScript))
            projectileScript.Initialize(direction, chargeRatio);
    }
    public void FireWeapon()
    {
        if (gameObject.CompareTag("Dead"))
        {
            Debug.Log("❌ Cannot shoot: Player is dead.");
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("❌ Cannot fire: no ammo.");
            return;
        }
        // 🔒 Block shooting if no weapon is equipped
        if (currentWeapon == null || string.IsNullOrEmpty(WeaponControllerID))
        {
            Debug.LogWarning("❌ Cannot fire: no weapon equipped.");
            return;
        }

        if (currentWeapon == null || firePoint == null || aimTarget == null)
            return;

        lastFireTime = Time.time;
        currentAmmo--;
        if (ammoRegenRoutine == null)
            ammoRegenRoutine = StartCoroutine(AmmoRegeneration());

        UpdateAmmoUI();

        int projectileCount = Mathf.Max(1, currentWeapon.projectileMultiplier);
        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 shootDirection = GetShootDirectionWithScatter();
            CmdShoot(firePoint.position, shootDirection, 0f); // 👈 No charge for regular shots
        }
    }

    private Vector3 GetShootDirectionWithScatter()
    {
        if (firePoint == null || aimTarget == null)
        {
            Debug.LogError("❌ GetShootDirectionWithScatter: FirePoint or AimTarget not assigned!");
            return Vector3.zero;
        }

        Vector3 shootDirection = (aimTarget.position - firePoint.position).normalized;
        Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(0f, scatterAngle), Random.insideUnitSphere);
        return (randomRotation * shootDirection).normalized;
    }

    [Command]
    private void CmdShoot(Vector3 position, Vector3 direction, float chargeRatio)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("❌ CmdShoot: No projectile prefab assigned!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(projectile, gameObject.scene);
        NetworkServer.Spawn(projectile);

        // ✅ Create context and apply effects
        if (projectile.TryGetComponent(out IProjectile projectileScript))
        {
            projectileScript.Initialize(direction, chargeRatio);
        }

        // ✅ Modular effect system
        if (currentWeapon.projectileEffects != null && currentWeapon.projectileEffects.Count > 0)
        {
            var context = new ProjectileContext();
            context.InitializeContext(projectile, direction, netIdentity, chargeRatio);

            foreach (var effect in currentWeapon.projectileEffects)
            {
                if (effect != null)
                    effect.ApplyEffect(context);
            }
        }
    }
}
