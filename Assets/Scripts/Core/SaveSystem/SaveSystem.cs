using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reads the save file once at startup and writes it at checkpoints, on scene changes and when quitting
// from the pause menu. There is no main menu, so an existing save is always continued.
[AutoCreatedSingleton]
public class SaveSystem : Singleton<SaveSystem>
{
    public const int CurrentVersion = 1;
    public static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    // The save that was on disk at startup; null means this is a new game
    public SaveData LoadedData { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (IsDuplicate) return;
        LoadedData = ReadSaveFile();
    }

    private static SaveData ReadSaveFile()
    {
        if (!File.Exists(SavePath))
            return null;
        try
        {
            SaveData data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SavePath));
            if (data != null && data.version > CurrentVersion)
                Debug.LogWarning($"Save file version {data.version} is newer than this build ({CurrentVersion}); loading what it can.");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not read save file {SavePath}, starting a new game: {e.Message}");
            BackUpUnreadableSave();
            return null;
        }
    }

    // Keeps a copy of an unreadable save before the next save overwrites it
    private static void BackUpUnreadableSave()
    {
        try
        {
            File.Copy(SavePath, SavePath + ".corrupt", true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not back up the unreadable save: {e.Message}");
        }
    }

    public void Save()
    {
        SaveData data = new SaveData
        {
            world = WorldStateManager.Instance.GetData(),
            player = CapturePlayer() ?? LoadedData?.player,
        };
        try
        {
            // Write a temp file first so a crash mid-write can't leave a half-written save
            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            if (File.Exists(SavePath))
                File.Replace(tempPath, SavePath, null);
            else
                File.Move(tempPath, SavePath);
            Debug.Log($"Game saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not write save file {SavePath}: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        foreach (string path in new[] { SavePath, SavePath + ".tmp" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        LoadedData = null;
    }

    // Deletes the save and restarts from the first scene with fresh persistent objects
    public void StartNewGame()
    {
        DeleteSave();
        // The player, inventory, UIs and managers live in the DontDestroyOnLoad scene (where this object
        // is too); clear them so Level0 recreates everything from scratch. Only our own objects:
        // packages (URP, Input System) can keep helpers there that would not come back.
        System.Reflection.Assembly gameAssembly = typeof(SaveSystem).Assembly;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.GetComponentsInChildren<MonoBehaviour>(true).Any(b => b != null && b.GetType().Assembly == gameAssembly))
                Destroy(root);
        }
        Time.timeScale = 1f;
        CoreUI.IsUIOpen = false;
        SceneManager.LoadScene(0);
    }

    private static PlayerSaveData CapturePlayer()
    {
        PersistentPlayerHealth health = PersistentPlayerHealth.Instance;
        if (health == null)
            return null; // No player this session (Play mode started outside Level0)

        GameRespawn respawn = GameRespawn.Instance;
        return new PlayerSaveData
        {
            gold = InventorySystem.Instance.gold,
            health = health.CurrentHealth,
            mana = health.TryGetComponent(out Mana mana) ? mana.CurrentMana : null,
            dungeonLevel = DungeonManager.Instance.DungeonLevel,
            equippedItemIds = ToItemIds(EquipmentSystem.Instance.EquippedItems),
            inventoryItemIds = ToItemIds(InventorySystem.Instance.items),
            checkpointScene = respawn.CheckpointScene,
            checkpointX = respawn.CheckpointPosition.x,
            checkpointY = respawn.CheckpointPosition.y,
        };
    }

    private static List<int> ToItemIds(IEnumerable<Item> items)
    {
        // Only items.json items have a meaningful Id; ScriptableObject items from ItemVarients all report 0
        foreach (Item item in items.Where(i => i != null && !(i is RuntimeItem)))
            Debug.LogWarning($"Item '{item.Name}' is not from items.json and can't be saved");
        return items.OfType<RuntimeItem>().Select(i => i.Id).ToList();
    }

    // Applies the loaded save to the persistent hero; returns false for a new game
    public bool RestorePlayer(Hero hero)
    {
        PlayerSaveData player = LoadedData?.player;
        if (player == null)
            return false;

        // Worn items first (each is added to the inventory and equipped from there), then the rest of the inventory
        ItemSystem.Instance.AddAndEquipOnPlayer(player.equippedItemIds.ToArray());
        ItemSystem.Instance.AddToPlayerInventory(player.inventoryItemIds.ToArray());
        InventorySystem.Instance.UpdateGold(player.gold - InventorySystem.Instance.gold);
        DungeonManager.Instance.DungeonLevel = player.dungeonLevel;

        // After equipping, so max health includes equipment bonuses; a save made while dead comes back at full health
        hero.Health.SetHealth(player.health > 0 ? player.health : hero.Health.MaxHealth);
        if (player.mana.HasValue && hero.TryGetComponent(out Mana mana))
            mana.SetMana(player.mana.Value);

        if (!string.IsNullOrEmpty(player.checkpointScene))
        {
            Vector3 checkpoint = new Vector3(player.checkpointX, player.checkpointY, hero.transform.position.z);
            GameRespawn.Instance.SetCheckpoint(player.checkpointScene, checkpoint);
            if (player.checkpointScene == SceneManager.GetActiveScene().name)
                hero.transform.position = checkpoint; // Continue from the checkpoint
        }
        return true;
    }
}
