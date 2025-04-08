using System.Diagnostics.SymbolStore;
using HarmonyLib;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Skating;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.ItemFramework;

[assembly: MelonInfo(typeof(TestingMod.Core), "TestingMod", "1.0.0", "airplanegobrr", null)]
[assembly: MelonGame("TVGS", "Schedule I")]


[HarmonyPatch(typeof(Skateboard_Equippable), "Mount")]
public static class MountPatch {
    public static void Postfix(Skateboard_Equippable __instance) {
        MelonLogger.Msg("Skateboard patch!");

        Skateboard activeSkateboard = __instance.ActiveSkateboard;
        if (activeSkateboard != null) {
            MelonLogger.Msg("On skateboard!");
            activeSkateboard.CurrentSpeed_Kmh = 20;
            activeSkateboard.TopSpeed_Kmh = 100;
            activeSkateboard.PushForceMultiplier = 50;
            activeSkateboard.JumpForce = 100;
            activeSkateboard.TurnSpeedBoost = 1000;
        }
    }
}


// If anyone gives me shit for my code format, suck my dick. I like bracket format stfu.

namespace TestingMod {
    public class Core : MelonMod {

        public bool shwoingGUI = false;
        private float startTime;
        private bool showBootText = true;

        public override void OnInitializeMelon() {
            LoggerInstance.Msg("Initialized.");

            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("Added Harmony patch!");



            startTime = Time.time;
            // The higher the value, the lower the priority.
        }

        public override void OnGUI() {
            GUIStyle centeredStyle = new(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                richText = true
            };

            if (showBootText) {
                float elapsed = Time.time - startTime;
                if (elapsed < 4f) {
                    GUI.Label(new Rect(0, 0, Screen.width, Screen.height + 500), "<b><color=red>Welcome!</color></b>", centeredStyle);
                } else {
                    showBootText = false;
                }
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");

            if (sceneName != "Main") return;

        }

        public override void OnLateUpdate() {
            if (Input.GetKeyDown(KeyCode.L)) {

                shwoingGUI = !shwoingGUI;
                LoggerInstance.Msg("L key!" + shwoingGUI);


                if (shwoingGUI) {
                    MelonEvents.OnGUI.Subscribe(DrawMenu, 100);
                } else {
                    MelonEvents.OnGUI.Unsubscribe(DrawMenu);
                }
            }

            if (Input.GetKeyDown(KeyCode.H)) {

                LoggerInstance.Msg("Raycast test");

                PlayerCamera cam = PlayerSingleton<PlayerCamera>.instance;
                PlayerInventory plrInv = PlayerSingleton<PlayerInventory>.Instance;


                RaycastHit hit;
                LayerMask mask = LayerMask.GetMask("Default"); // or whichever layers you want to include

                if (cam.LookRaycast(10f, out hit, mask)) {
                    GameObject lookedAtObject = hit.collider.gameObject;

                    LoggerInstance.Msg("Name is: " + lookedAtObject.gameObject.name);
                    LoggerInstance.Msg("Parent name is: " + lookedAtObject.transform.parent.gameObject.name);

                    if (lookedAtObject.transform.parent.gameObject.name == "DeliveryVehicles") { // Van
                        GameObject van = lookedAtObject;
                        StorageEntity vanInv = van.GetComponent<StorageEntity>();

                        Il2CppSystem.Collections.Generic.List<ItemSlot> vanItems = vanInv.ItemSlots;

                        Il2CppSystem.Collections.Generic.List<ItemSlot> playerItems = plrInv.GetAllInventorySlots();

                        LoggerInstance.Msg("[VAN] Got requried inventories");

                        foreach (var vanItem in vanItems) {
                            if (vanItem.ItemInstance == null) { continue; }

                            foreach (var playerItem in playerItems) {
                                if (playerItem.ItemInstance != null) { continue; }

                                LoggerInstance.Msg("[VAN] Found empty slot!");
                                playerItem.SetStoredItem(vanItem.ItemInstance.GetCopy());
                                playerItem.ItemInstance.SetQuantity(vanItem.Quantity);
                                vanItem.ItemInstance.SetQuantity(0);
                            }
                        }
                    } else if (lookedAtObject.name == "Trigger") { // Shelf

                        GameObject shelf = lookedAtObject.transform.parent.gameObject;
                        LoggerInstance.Msg("Looking at: " + shelf.name);

                        StorageEntity shelfInv = shelf.GetComponent<StorageEntity>();
                        Il2CppSystem.Collections.Generic.List<ItemSlot> shelfItems = shelfInv.ItemSlots;

                        // NetworkObject myNetObj = shelf.GetComponent<NetworkObject>();
                        // if (shelfInv.ItemSlots[0].IsLocked) {
                        //     shelfInv.ItemSlots[0].RemoveLock();
                        // } else {
                        //     shelfInv.ItemSlots[0].ApplyLock(myNetObj, "LockedByScript");
                        // }
                        // LoggerInstance.Msg(shelfInv.ItemSlots[0].IsLocked);

                        Il2CppSystem.Collections.Generic.List<ItemSlot> playerItems = plrInv.GetAllInventorySlots();


                        foreach (var shelfItem in shelfItems) {

                            ItemInstance itemWanted = null;
                            int shelfItemIndex = shelfItem.SlotIndex;

                            if (shelfItem.ItemInstance == null) {
                                if (shelfItem.SlotIndex != 0) {
                                    int backIndex = shelfItem.SlotIndex - 1;

                                    if (backIndex >= 0 && backIndex < shelfItems._size) {
                                        ItemSlot shelfSlotBefore = shelfItems[backIndex];

                                        if (shelfSlotBefore != null && shelfSlotBefore.ItemInstance != null) {
                                            itemWanted = shelfSlotBefore.ItemInstance.GetCopy();
                                            itemWanted.Quantity = 0;
                                            shelfItemIndex = shelfSlotBefore.SlotIndex;
                                        }
                                    }
                                } else {
                                    continue;
                                }
                            } else {
                                itemWanted = shelfItem.ItemInstance;
                            }

                            if (itemWanted == null) {
                                continue;
                            }

                            LoggerInstance.Msg("itemWanted " + itemWanted.Name);

                            foreach (var playerItem in playerItems) {
                                // item.ChangeQuantity(-1);
                                if (playerItem.ItemInstance == null) {
                                    continue;
                                }

                                if (itemWanted.Name == playerItem.ItemInstance.Name) {
                                    int shelfCurrent = itemWanted.Quantity;
                                    int shelfMax = itemWanted.StackLimit;
                                    int playerAmount = playerItem.ItemInstance.Quantity;

                                    int spaceAvailable = shelfMax - shelfCurrent;

                                    if (spaceAvailable <= 0) {
                                        LoggerInstance.Msg("[Shelf] Slot is full!");
                                        continue; // Shelf is full, skip
                                    }

                                    int transferAmount = Mathf.Min(spaceAvailable, playerAmount);
                                    LoggerInstance.Msg("[Shelf] Items moving: "+transferAmount);

                                    // Transfer items to the shelf
                                    shelfItem.SetStoredItem(playerItem.ItemInstance.GetCopy());
                                    itemWanted.ChangeQuantity(transferAmount);

                                    int nextIndex = shelfItemIndex + 1;

                                    // Make sure the next index is valid
                                    if (nextIndex < shelfItems._size) {
                                        ItemSlot nextShelfItem = shelfItems[nextIndex];

                                        // If the next slot is empty, move the remainder there
                                        if (nextShelfItem.ItemInstance == null) {
                                            nextShelfItem.SetStoredItem(playerItem.ItemInstance.GetCopy()); // Use a copy if needed
                                            nextShelfItem.SetQuantity(playerAmount - transferAmount);
                                            playerItem.SetQuantity(0);
                                        } else {
                                            // If it's already holding an item, just update the player's quantity (can't transfer)
                                            playerItem.SetQuantity(playerAmount - transferAmount);
                                        }
                                    } else {
                                        // No next slot exists — return remainder to player
                                        playerItem.SetQuantity(playerAmount - transferAmount);
                                    }

                                }



                            }
                        }

                        // plrInv.IndexAllSlots(0).IsLocked = true;
                    } else {
                        LoggerInstance.Msg("Nothing hit");

                    }
                    // 

                    // if (plrInv.IndexAllSlots(0).IsEquipped) {
                    //    plrInv.IndexAllSlots(0).Unequip();
                    //} else {
                    //    plrInv.IndexAllSlots(0).Equip();
                    //}


                }

                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Q)) {
                    // quit game
                    LoggerInstance.Msg("Force quit!");
                    Application.Quit();
                }
            }
        }


        private void DrawMenu() {

            if (GUI.Button(new Rect(100, 100, 100, 35), "Force quit!")) {
                // This runs when the button is clicked
                LoggerInstance.Msg("Hello button was clicked!");
                Application.Quit();
            }

            GUIStyle centeredStyle = new(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                richText = true
            };

            GUI.Label(new Rect(0, 0, Screen.width, Screen.height + 500), "<b><color=cyan><size=100>Menu Open</size></color></b>", centeredStyle);

        }

    }
}
