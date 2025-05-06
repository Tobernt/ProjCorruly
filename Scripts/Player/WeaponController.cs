using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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

    public enum FireMode
    {
        Tap,
        Burst,
        Auto,
        ChargedShot
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

        if (Input.GetKeyDown(KeyCode.R))
        {
            AttemptManualReload();
        }

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
                    burstFireRoutine = StartCoroutine(BurstFireRoutine());
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
                Debug.Log("❌ Burst stopped: out of ammo, starting reload.");
                StartReload();
                break;
            }

            FireWeapon();
            shotsFired++;
            yield return new WaitForSeconds(1f / currentWeapon.attackSpeed);
        }

        burstFireRoutine = null;
    }


    private void AttemptManualReload()
    {
        if (isReloading)
        {
            Debug.Log("🔄 Already reloading.");
            return;
        }

        if (currentAmmo >= currentWeapon.magSize)
        {
            Debug.Log("🔋 Magazine already full.");
            return;
        }

        Debug.Log("🔁 Manual reload triggered.");
        StartReload();
    }

    public void UpdateWeapon()
    {
        var tracker = GetComponent<PlayerEquipmentTracker>();
        if (tracker == null) return;
        WeaponControllerID = tracker.GetEquippedWeaponID();

        Debug.Log($"🔄 Updating WeaponController. New WeaponControllerID: {WeaponControllerID}");

        if (string.IsNullOrEmpty(WeaponControllerID))
        {
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

        currentAmmo = currentWeapon.magSize;
        fireMode = (FireMode)currentWeapon.fireMode;

        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(true); // ✅ Show when a weapon is equipped
            UpdateAmmoUI();
        }

        Debug.Log($"✅ Weapon updated: {currentWeapon.itemName}, Ammo: {currentAmmo}");
    }

    private void FireChargedShot(float chargeTime)
    {
        if (currentAmmo <= 0 || isReloading || gameObject.CompareTag("Dead")) return;

        Debug.Log($"🔋 Charged shot fired with {chargeTime:F2}s charge!");

        CancelReload(); // Optional: cancel reload

        currentAmmo--;
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
        // 🔒 Block shooting if no weapon is equipped
        if (currentWeapon == null || string.IsNullOrEmpty(WeaponControllerID))
        {
            Debug.LogWarning("❌ Cannot fire: no weapon equipped.");
            return;
        }

        if (currentWeapon == null || firePoint == null || aimTarget == null)
            return;

        // ✅ If currently reloading, cancel only if there's ammo
        if (isReloading)
        {
            if (currentAmmo > 0)
            {
                Debug.Log("⏹️ Reload canceled due to shooting.");
                CancelInvoke(nameof(FinishReload));
                isReloading = false;
            }
            else
            {
                Debug.Log("⏳ Still reloading, no ammo to cancel.");
                return; // 🔒 Block firing while reloading with 0 ammo
            }
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("❌ No ammo, starting reload.");
            StartReload();
            return;
        }

        lastFireTime = Time.time;
        currentAmmo--;
        UpdateAmmoUI();

        int projectileCount = Mathf.Max(1, currentWeapon.projectileMultiplier);
        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 shootDirection = GetShootDirectionWithScatter();
            CmdShoot(firePoint.position, shootDirection, 0f); // 👈 No charge for regular shots
        }
    }

    private void CancelReload()
    {
        CancelInvoke(nameof(FinishReload));
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

    private void StartReload()
    {
        if (isReloading || currentAmmo == currentWeapon.magSize) return;

        CancelReload(); // ❌ Ensure no old reload is pending
        isReloading = true;
        Debug.Log($"🔄 Reloading... Time: {currentWeapon.reloadSpeed}s");

        Invoke(nameof(FinishReload), currentWeapon.reloadSpeed);
    }


    private void FinishReload()
    {
        isReloading = false;
        currentAmmo = currentWeapon.magSize;
        Debug.Log($"✅ Reloaded! Ammo: {currentAmmo}/{currentWeapon.magSize}");
        UpdateAmmoUI();
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
