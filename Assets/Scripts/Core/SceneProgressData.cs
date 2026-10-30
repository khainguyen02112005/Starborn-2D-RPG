using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneProgressData
{
    public string sceneName;
    public WorldEnemySaveData[] enemies;
    public ChestSaveData[] chests;
    public WorldPickupSaveData[] pickups;
}

[Serializable]
public class WorldEnemySaveData
{
    public string id;
    public Vector3 position;
    public int health;
    public bool defeated;
    public bool encounterActive;
    public bool spawnedMinion;
}

[Serializable]
public class ChestSaveData
{
    public string id;
    public InventoryItemSaveData[] items;
}

[Serializable]
public class WorldPickupSaveData
{
    public string kind;
    public string itemId;
    public Vector3 position;
    public int amount;
    public int upgrade;
    public int durability = 100;

    public static WorldPickupSaveData[] CaptureAll()
    {
        var result = new List<WorldPickupSaveData>();
        foreach (ExperiencePickup xp in UnityEngine.Object.FindObjectsByType<ExperiencePickup>())
            result.Add(new WorldPickupSaveData { kind = "xp", position = xp.transform.position, amount = xp.Experience });
        foreach (GameObject coin in GameObject.FindGameObjectsWithTag("Coin"))
            result.Add(new WorldPickupSaveData { kind = "coin", position = coin.transform.position });
        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item"))
        {
            MonsterLootData loot = item.GetComponent<MonsterLootData>();
            if (loot != null && loot.itemData != null)
                result.Add(new WorldPickupSaveData { kind = "item", position = item.transform.position, itemId = ItemCatalog.GetId(loot.itemData), upgrade = loot.upgradeLevel, durability = loot.durability });
        }
        return result.ToArray();
    }

    public static void RestoreAll(WorldPickupSaveData[] pickups)
    {
        if (pickups == null) return;
        foreach (ExperiencePickup xp in UnityEngine.Object.FindObjectsByType<ExperiencePickup>()) UnityEngine.Object.Destroy(xp.gameObject);
        foreach (GameObject coin in GameObject.FindGameObjectsWithTag("Coin")) UnityEngine.Object.Destroy(coin);
        foreach (GameObject item in GameObject.FindGameObjectsWithTag("Item")) UnityEngine.Object.Destroy(item);
        foreach (WorldPickupSaveData saved in pickups)
        {
            if (!EnemyLoot.TryResolveDropPosition(saved.position, out Vector3 position)) continue;
            if (saved.kind == "xp") { ExperiencePickup.Spawn(position, saved.amount); continue; }
            if (saved.kind == "coin")
            {
                GameObject prefab = Resources.Load<GameObject>("CombatVFX/CoinPickup");
                if (prefab != null) UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                continue;
            }
            ItemData data = ItemCatalog.Load()?.Find(saved.itemId);
            if (data == null) continue;
            var drop = new GameObject("Loot_" + data.name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(MonsterLootData));
            drop.transform.position = position;
            drop.tag = "Item";
            drop.GetComponent<SpriteRenderer>().sprite = data.itemIcon;
            drop.GetComponent<SpriteRenderer>().sortingOrder = 5;
            drop.GetComponent<BoxCollider2D>().isTrigger = true;
            drop.GetComponent<MonsterLootData>().itemData = data;
            drop.GetComponent<MonsterLootData>().upgradeLevel = saved.upgrade;
            drop.GetComponent<MonsterLootData>().durability = saved.durability;
        }
    }
}
