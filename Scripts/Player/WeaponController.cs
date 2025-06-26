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
    [SyncVar(hook = nameof(OnWeaponChanged))]
    public string WeaponControllerID;

    [Header("UI")]
    public TextMeshProUGUI ammoText;
    [Header("References")]
    public Transform firePoint;
    public Transform aimTarget;
    public GameObject projectilePrefab;
    public float scatterAngle;
    private ItemSO currentWeapon;
    private bool isReloading = false;
    private float chargeStartTime;
    private bool isCharging;
    private float maxChargeTime = 5f;
    private float fireRate = 0.1f;
    private float lastFireTime;
    private int currentAmmo; // ✅ Tracks remaining ammo without modifying ItemSO
    private FireMode fireMode = FireMode.Tap; // Default mode
    private Coroutine burstFireRoutine;
    public bool isCombatMode = false;
    private Coroutine ammoRegenRoutine;
    private Dictionary<string, int> weaponAmmo = new();
    private SpellDeckExecutor spellExecutor;
    private SpellDeckUI spellDeckUI;

    private void OnWeaponChanged(string oldID, string newID)
    {
        Debug.Log($"[HOOK] Weapon changed from {oldID} → {newID}");
        currentWeapon = ItemDatabaseSO.Instance.GetItemById(newID);

        if (currentWeapon == null)
        {
            Debug.LogWarning("⚠️ Weapon ID is invalid or null.");
            return;
        }

        // ✅ Always assign spellExecutor, regardless of isLocalPlayer
        if (currentWeapon.projectileEffects != null && currentWeapon.projectileEffects.Count > 0)
        {
            spellExecutor = new SpellDeckExecutor(currentWeapon.projectileEffects, currentWeapon.shuffle);
        }
        else
        {
            spellExecutor = null;
        }

        if (isLocalPlayer)
        {
            spellDeckUI?.ShowDeck(spellExecutor?.CurrentDeck ?? new(), spellExecutor?.CurrentIndex ?? 0);
            UpdateAmmoUI();
        }

        Debug.Log($"✅ Loaded weapon: {currentWeapon.itemName}, Effects: {currentWeapon.projectileEffects?.Count ?? 0}");
    }

    public enum FireMode
    {
        Tap,
        Burst,
        Auto,
        ChargedShot
    }
    [Command]
    public void CmdEquipWeapon(string weaponId)
    {

        if (WeaponControllerID != weaponId)
        {
            WeaponControllerID = weaponId;
        }
        else
        {
            // 👇 Force call manually if value didn’t change (local only)
            if (isLocalPlayer)
                OnWeaponChanged(weaponId, weaponId);
        }
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
        ammoUIPrefabRoot = GameObject.Find("PlayerHUD");
        StartCoroutine(WaitForAmmoText());

        // ✅ Force manual weapon init if value already synced
        if (!string.IsNullOrEmpty(WeaponControllerID))
        {
            Debug.Log("🔁 Forcing local OnWeaponChanged due to pre-set SyncVar.");
            OnWeaponChanged("", WeaponControllerID);
        }
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

    private void LateUpdate()
    {
        if (!isLocalPlayer || spellExecutor == null || spellDeckUI == null) return;

        spellDeckUI.ShowDeck(spellExecutor.CurrentDeck, spellExecutor.CurrentIndex);
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

        // Save ammo from current weapon if switching away
        if (!string.IsNullOrEmpty(WeaponControllerID) && currentWeapon != null)
            weaponAmmo[WeaponControllerID] = currentAmmo;

        string newID = tracker.GetEquippedWeaponID();
        WeaponControllerID = newID;
        Debug.Log($"🔄 Updating WeaponController. New WeaponControllerID: {WeaponControllerID}");

        if (string.IsNullOrEmpty(WeaponControllerID))
        {
            currentWeapon = null;
            currentAmmo = 0;
            spellExecutor = null;

            if (ammoRegenRoutine != null)
            {
                StopCoroutine(ammoRegenRoutine);
                ammoRegenRoutine = null;
            }

            if (ammoText != null)
            {
                ammoText.text = "";
                ammoText.gameObject.SetActive(false);
            }

            if (spellDeckUI != null)
                spellDeckUI.ShowDeck(new List<ProjectileEffect>(), 0);

            Debug.Log("❌ Weapon unequipped. Resetting weapon data.");
            return;
        }

        // Get new weapon data
        currentWeapon = ItemDatabaseSO.Instance.GetItemById(WeaponControllerID);
        if (currentWeapon == null)
        {
            Debug.LogError($"❌ Item ID {WeaponControllerID} not found in database.");
            return;
        }

        // Restore saved ammo if it exists
        if (!weaponAmmo.TryGetValue(WeaponControllerID, out currentAmmo))
        {
            currentAmmo = currentWeapon.magSize;
            weaponAmmo[WeaponControllerID] = currentAmmo;
        }

        fireMode = (FireMode)currentWeapon.fireMode;

        if (isLocalPlayer)
        {
            // Force server to recognize this weapon for spawning
            CmdEquipWeapon(WeaponControllerID);

            // Initialize local spell system
            if (currentWeapon.projectileEffects != null && currentWeapon.projectileEffects.Count > 0)
            {
                spellExecutor = new SpellDeckExecutor(currentWeapon.projectileEffects, currentWeapon.shuffle);
            }
            else
            {
                spellExecutor = null;
            }

            spellDeckUI?.ShowDeck(currentWeapon.projectileEffects ?? new List<ProjectileEffect>(), spellExecutor?.CurrentIndex ?? 0);
        }


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
        {
            var context = new ProjectileContext();
            context.InitializeContext(projectile, direction, netIdentity, chargeRatio);
            context.chainedEffects = currentWeapon.projectileEffects;

            projectileScript.Initialize(direction, chargeRatio, context);
        }

    }

    [Command]
    private void CmdCastSpellDeck(Vector3 firePosition, Vector3 direction)
    {
        if (currentWeapon == null)
        {
            Debug.LogError("❌ CmdCastSpellDeck: currentWeapon is NULL!");
            return;
        }

        if (spellExecutor == null)
        {
            Debug.LogWarning("❌ CmdCastSpellDeck: spellExecutor is NULL! Reinitializing...");
            spellExecutor = new SpellDeckExecutor(currentWeapon.projectileEffects, currentWeapon.shuffle);
        }
        spellExecutor.CastNextFromServer(firePosition, direction, netIdentity, gameObject.scene);
    }


    public void FireWeapon()
    {
        if (gameObject.CompareTag("Dead"))
        {
            Debug.Log("❌ Cannot shoot: Player is dead.");
            return;
        }
        if (spellExecutor == null)
        {
            Debug.LogError("❌ spellExecutor is NULL on client! WeaponID: " + WeaponControllerID);
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("❌ Cannot fire: no ammo.");
            return;
        }
        // Block shooting if no weapon is equipped
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
        CmdCastSpellDeck(firePoint.position, GetShootDirectionWithScatter());
        UpdateAmmoUI();

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

        if (projectile.TryGetComponent(out IProjectile projectileScript))
        {
            var context = new ProjectileContext();
            context.InitializeContext(projectile, direction, netIdentity, chargeRatio);
            context.chainedEffects = currentWeapon.projectileEffects;

            // Initialize projectile with context
            projectileScript.Initialize(direction, chargeRatio, context);

            // Apply all effects (including chaining)
            if (currentWeapon.projectileEffects != null && currentWeapon.projectileEffects.Count > 0)
            {
                foreach (var effect in currentWeapon.projectileEffects)
                {
                    if (effect != null)
                        effect.ApplyEffect(context);
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ CmdShoot: Spawned projectile has no IProjectile implementation.");
        }
    }
}
