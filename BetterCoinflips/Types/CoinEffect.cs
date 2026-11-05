using System;
using System.Collections.Generic;
using System.Linq;
using BetterCoinflips.Configs;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Waves;
using InventorySystem.Items.Firearms.Attachments;
using MEC;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using UnityEngine;
using Player = Exiled.API.Features.Player;

namespace BetterCoinflips.Types
{
    public class CoinFlipEffect
    {
        private static Config Config => Plugin.Instance.Config;
        private static Configs.Translations Translations => Plugin.Instance.Translation;

        private static readonly System.Random Rd = new();
        public Action<Player> Execute { get; set; }
        public string Message { get; set; }

        public CoinFlipEffect(string message, Action<Player> execute)
        {
            Execute = execute;
            Message = message;
        }

        private static readonly Dictionary<string, string> _scpNames = new()
        {
            {
                "1 7 3",
                "SCP-173"
            },
            {
                "9 3 9",
                "SCP-939"
            },
            {
                "0 9 6",
                "SCP-096"
            },
            {
                "0 7 9",
                "SCP-079"
            },
            {
                "0 4 9",
                "SCP-049"
            },
            {
                "1 0 6",
                "SCP-106"
            }
        };
        private static CoinFlipEffect KeycardReward()
        {
            bool red = Config.RedCardChance >= Rd.Next(1, 101);
            return new CoinFlipEffect(red ? Translations.RedCardMessage : Translations.ContainmentEngineerCardMessage, player => Pickup.CreateAndSpawn(red ? ItemType.KeycardFacilityManager : ItemType.KeycardContainmentEngineer, player.Position, Quaternion.identity));
        }

        // Damage that always leaves the player alive.
        private static void Sting(Player player, float amount) => player.Health = Math.Max(1, player.Health - amount);

        private static void RandomItem(Player player)
        {
            if (Config.ItemsToGive?.Count > 0)
                Item.Create(Config.ItemsToGive.ToList().RandomItem()).CreatePickup(player.Position);
        }

        private static CoinFlipEffect CustomItemReward()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Translations.RandomItemMessage, player =>
            {
                string name = BrainlessContent.GiveRandomItem(player, Config.CustomItems, Config.CustomItemsIgnoreSupply);
                if (name == null)
                    RandomItem(player);
                else
                    effect.Message = Translations.CustomItemMessage.Replace("{item}", name);
            });
            return effect;
        }

        private static CoinFlipEffect CustomRoleReward()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Translations.RandomItemMessage, player =>
            {
                string name = BrainlessContent.GrantRandomRole(player, Config.CustomRoles);
                if (name == null)
                    RandomItem(player);
                else
                    effect.Message = Translations.CustomRoleMessage.Replace("{role}", name);
            });
            return effect;
        }

        private static CoinFlipEffect CursedItemPunishment()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Translations.SmallDamageMessage, player =>
            {
                string name = BrainlessContent.GiveRandomItem(player, Config.CursedCustomItems, Config.CustomItemsIgnoreSupply);
                if (name == null)
                    Sting(player, 15);
                else
                    effect.Message = Translations.CursedItemMessage.Replace("{item}", name);
            });
            return effect;
        }

        // GoodEffects list
        public static List<CoinFlipEffect> GoodEffects => new()
        {
            // 0: Spawns a red keycard or containment enginner
            KeycardReward(),
            // 1: Spawns a medkit and painkillers for the player.
            new CoinFlipEffect(Translations.MediKitMessage, player =>
            {
                Pickup.CreateAndSpawn(ItemType.Medkit, player.Position, new Quaternion());
                Pickup.CreateAndSpawn(ItemType.Painkillers, player.Position, new Quaternion());
            }),
            // 2: Teleports the player to the escape secondary door.
            new CoinFlipEffect(Translations.TpToEscapeMessage, player =>
            {
                player.Teleport(Door.Get(DoorType.EscapeSecondary));
            }),
            // 3: Heals the player by 25 health points.
            new CoinFlipEffect(Translations.MagicHealMessage, player =>
            {
                player.Heal(25);
            }),
            // 4: Increases the player's health by 10%.
            new CoinFlipEffect(Translations.HealthIncreaseMessage, player =>
            {
                player.Health *= 1.1f;
            }),
            // 5: Spawns SCP-268 (Neat Hat) for the player.
            new CoinFlipEffect(Translations.NeatHatMessage, player =>
            {
                Pickup.CreateAndSpawn(ItemType.SCP268, player.Position, new Quaternion());
            }),
            // 6: Applies a random good effect to the player.
            new CoinFlipEffect(Translations.RandomGoodEffectMessage, player =>
            {
                if (Config.GoodEffects == null || Config.GoodEffects.Count == 0)
                    return;
                var effect = Config.GoodEffects.ToList().RandomItem();
                player.EnableEffect(effect, 5, true);
                Log.Debug($"Chosen random effect: {effect}");
            }),
            // 7: Spawns a Logicer with one ammo for the player.
            new CoinFlipEffect(Translations.OneAmmoLogicerMessage, player =>
            {
                Firearm gun = (Firearm)Item.Create(ItemType.GunLogicer);
                gun.BarrelAmmo = 1;
                gun.CreatePickup(player.Position);
            }),
            // 8: Spawns SCP-2176 (lightbulb) for the player.
            new CoinFlipEffect(Translations.LightbulbMessage, player =>
            {
                Pickup.CreateAndSpawn(ItemType.SCP2176, player.Position, new Quaternion());
            }),
            // 9: Spawns pink candy (SCP-330) for the player.
            new CoinFlipEffect(Translations.PinkCandyMessage, player =>
            {
                Scp330 candy = (Scp330)Item.Create(ItemType.SCP330);
                candy.AddCandy(InventorySystem.Items.Usables.Scp330.CandyKindID.Pink);
                candy.CreatePickup(player.Position);
            }),
            // 10: Spawns a customized revolver with attachments for the player.
            new CoinFlipEffect(Translations.BadRevoMessage, player =>
            {
                Firearm revo = (Firearm)Item.Create(ItemType.GunRevolver);
                revo.AddAttachment(new[] { AttachmentName.CylinderMag7, AttachmentName.ShortBarrel, AttachmentName.ScopeSight });
                revo.CreatePickup(player.Position);
            }),
            // 11: Spawns a MicroHID with no energy for the player.
            new CoinFlipEffect(Translations.EmptyHidMessage, player =>
            {
                MicroHIDPickup item = (MicroHIDPickup)Pickup.Create(ItemType.MicroHID);
                item.Position = player.Position;
                item.Spawn();
                item.Energy = 0;
            }),
            // 12: Forces a respawn wave of the team that has more ticketes
            new CoinFlipEffect(Translations.ForceRespawnMessage, player =>
            {
                if (WaveManager.Waves.Any())
                    Respawn.ForceWave(WaveManager.Waves.RandomItem());
            }),
            // 13: Changes the player's size
            new CoinFlipEffect(Translations.SizeChangeMessage, player =>
            {
                player.Scale = new Vector3(1.13f, 0.5f, 1.13f);
            }),
            // 14: Spawns a random item for the player.
            new CoinFlipEffect(Translations.RandomItemMessage, RandomItem),
            // 15: Brainless: gives a custom item or weapon (falls back to a random item).
            CustomItemReward(),
            // 16: Brainless: turns the player into a fitting custom role (falls back to a random item).
            CustomRoleReward(),
        };
        // BadEffects list
        public static List<CoinFlipEffect> BadEffects => new()
        {
            // 0: Reduces player's health by 30%
            new CoinFlipEffect(Translations.HpReductionMessage, player =>
            {
                // Never lethal: a coin should not send anyone to spectator.
                player.Health = Math.Max(1, player.Health * 0.7f);
            }),
            // 1: Teleports the player to the class D cells.
            new CoinFlipEffect(Warhead.IsDetonated ? Translations.SmallDamageMessage : Translations.TpToClassDCellsMessage, player =>
            {
                // After detonation the facility is lethal, so take a small hit instead.
                if (Warhead.IsDetonated)
                {
                    Sting(player, 15);
                    return;
                }

                player.DropHeldItem();
                player.Teleport(Door.Get(DoorType.PrisonDoor));
            }),
            // 2: Applies a random bad effect to the player.
            new CoinFlipEffect(Translations.RandomBadEffectMessage, player =>
            {
                if (Config.BadEffects == null || Config.BadEffects.Count == 0)
                    return;
                var effect = Config.BadEffects.ToList().RandomItem();
                //prevents players from staying in PD infinitely
                if (effect == EffectType.PocketCorroding)
                    player.EnableEffect(EffectType.PocketCorroding);
                else
                    player.EnableEffect(effect, 5, true);
                Log.Debug($"Chosen random effect: {effect}");
            }),
            // 3: Starts or stops the warhead based on its state.
            new CoinFlipEffect(Warhead.IsDetonated || !Warhead.IsInProgress ? Translations.WarheadStartMessage : Translations.WarheadStopMessage, player =>
            {
                if (Warhead.IsDetonated || !Warhead.IsInProgress)
                    Warhead.Start();
                else
                    Warhead.Stop();
            }),
            // 4: Turns off all lights
            new CoinFlipEffect(Translations.LightsOutMessage, player =>
            {
                Map.TurnOffAllLights(Config.MapBlackoutTime);
            }),
            // 5: Spawns a live HE grenade
            new CoinFlipEffect(Translations.LiveGrenadeMessage, player =>
            {
                ExplosiveGrenade grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
                grenade.FuseTime = (float)Config.LiveGrenadeFuseTime;
                grenade.SpawnActive(player.Position + Vector3.up, player);
            }),
            // 6: Spawns a flash grenade with a short fuse time, sets the flash owner to the player so that it hopefully blinds people
            new CoinFlipEffect(Translations.TrollFlashMessage, player =>
            {
                FlashGrenade flash = (FlashGrenade)Item.Create(ItemType.GrenadeFlash, player);
                flash.FuseTime = 1f;
                flash.SpawnActive(player.Position);
            }),
            // 7: Teleports the player to a random SCP or inflicts damage if no SCPs exist.
            new CoinFlipEffect(Player.Get(Side.Scp).Any(x => x.Role.Type != RoleTypeId.Scp079) ? Translations.TpToRandomScpMessage : Translations.SmallDamageMessage, player =>
            {
                if (Player.Get(Side.Scp).Any(x => x.Role.Type != RoleTypeId.Scp079))
                {
                    Player scpPlayer = Player.Get(Side.Scp).Where(x => x.Role.Type != RoleTypeId.Scp079).ToList().RandomItem();
                    player.Position = scpPlayer.Position;
                    return;
                }

                Sting(player, 15);
            }),
            // 8: Sets player hp to 1 (never lethal)
            new CoinFlipEffect(Translations.HugeDamageMessage, player =>
            {
                player.Health = 1;
            }),
            // 9: Spawns a primed SCP-244 vase for the player.
            new CoinFlipEffect(Translations.PrimedVaseMessage, player =>
            {
                Scp244 vase = (Scp244)Item.Create(ItemType.SCP244a);
                vase.Primed = true;
                vase.CreatePickup(player.Position);
            }),
            // 10: Spawns a tantrum on the player Keywords: shit spawn create
            new CoinFlipEffect(Translations.ShitPantsMessage, player =>
            {
                player.PlaceTantrum();
            }),
            // 11: Broadcasts a fake SCP termination message.
            new CoinFlipEffect(Translations.FakeScpKillMessage, player =>
            {
                var scpName = _scpNames.ToList().RandomItem();
                Exiled.API.Features.Cassie.MessageTranslated($"scp {scpName.Key} successfully terminated by automatic security system", $"{scpName.Value} successfully terminated by Automatic Security System.");
            }),
            // 12: Forceclass the player to a random scp from the list Keywords: scp fc forceclass
            new CoinFlipEffect(Translations.TurnIntoScpMessage, player =>
            {
                player.DropItems();
                player.Scale = new Vector3(1, 1, 1);
                if (Config.ValidScps == null || Config.ValidScps.Count == 0)
                    return;
                var randomScp = Config.ValidScps.ToList().RandomItem();
                var before = player.Role.Base;
                player.Role.Set(randomScp, RoleSpawnFlags.AssignInventory);
                BrainlessContent.RollAfterClassChange(player, before);
                //prevents the player from staying in PD forever
                if (player.CurrentRoom.Type == RoomType.Pocket)
                    player.EnableEffect(EffectType.PocketCorroding);
            }),
            // 13: Resets player's inventory
            new CoinFlipEffect(Translations.InventoryResetMessage, player =>
            {
                player.DropHeldItem();
                player.ClearInventory();
            }),
            // 14: Flips the players role to the opposite
            new CoinFlipEffect(Translations.ClassSwapMessage, player =>
            {
                player.DropItems();
                var before = player.Role.Base;
                switch (player.Role.Type)
                {
                    case RoleTypeId.Scientist:
                        player.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.ClassD:
                        player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.ChaosConscript:
                    case RoleTypeId.ChaosRifleman:
                        player.Role.Set(RoleTypeId.NtfSergeant, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.ChaosMarauder:
                    case RoleTypeId.ChaosRepressor:
                        player.Role.Set(RoleTypeId.NtfCaptain, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.FacilityGuard:
                        player.Role.Set(RoleTypeId.ChaosRifleman, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.NtfPrivate:
                    case RoleTypeId.NtfSergeant:
                    case RoleTypeId.NtfSpecialist:
                        player.Role.Set(RoleTypeId.ChaosRifleman, RoleSpawnFlags.AssignInventory);
                        break;
                    case RoleTypeId.NtfCaptain:
                        List<RoleTypeId> roles = new List<RoleTypeId>
                        {
                            RoleTypeId.ChaosMarauder,
                            RoleTypeId.ChaosRepressor
                        };
                        player.Role.Set(roles.RandomItem(), RoleSpawnFlags.AssignInventory);
                        break;
                }

                BrainlessContent.RollAfterClassChange(player, before);

                //prevents the player from staying in PD forever
                if (player.CurrentRoom.Type == RoomType.Pocket)
                {
                    player.EnableEffect(EffectType.PocketCorroding);
                }
            }),
            // 15: Spawns an HE grenade with a very short fuse time
            new CoinFlipEffect(Translations.InstantExplosionMessage, player =>
            {
                ExplosiveGrenade instaBoom = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
                instaBoom.FuseTime = 0.1f;
                instaBoom.SpawnActive(player.Position, player);
            }),
            // 16: Swaps positions with another random player
            new CoinFlipEffect(Player.List.Count(x => x.IsAlive && !Config.PlayerSwapIgnoredRoles.Contains(x.Role.Type)) <= 1 ? Translations.PlayerSwapIfOneAliveMessage : Translations.PlayerSwapMessage, player =>
            {
                var playerList = Player.List.Where(x => x.IsAlive && !Config.PlayerSwapIgnoredRoles.Contains(x.Role.Type)).ToList();
                playerList.Remove(player);
                if (playerList.IsEmpty())
                {
                    return;
                }

                var targetPlayer = playerList.RandomItem();
                var pos = targetPlayer.Position;
                targetPlayer.Teleport(player.Position);
                player.Teleport(pos);
                EventHandlers.SendBroadcast(targetPlayer, Translations.PlayerSwapMessage);
            }),
            // 17: kicks the player
            new CoinFlipEffect(Translations.KickMessage, player =>
            {
                //delay so the broadcast can be sent to the player and doesn't throw NRE
                Plugin.Instance.Later(1f, () =>
                {
                    if (player.IsConnected)
                        player.Kick(Config.KickReason);
                });
            }),
            // 18: swap with a spectator
            new CoinFlipEffect(Player.List.Where(x => x.Role.Type == RoleTypeId.Spectator).IsEmpty() ? Translations.SpectSwapNoSpectsMessage : Translations.SpectSwapPlayerMessage, player =>
            {
                var spectList = Player.List.Where(x => x.Role.Type == RoleTypeId.Spectator).ToList();
                if (spectList.IsEmpty())
                {
                    return;
                }

                var spect = spectList.RandomItem();
                spect.Role.Set(player.Role.Type, RoleSpawnFlags.None);
                spect.Teleport(player);
                spect.Health = player.Health;
                var items = player.Items.ToList();
                var ammo = player.Ammo.ToDictionary(entry => entry.Key.GetAmmoType(), entry => entry.Value);
                player.ClearInventory(false);
                foreach (var item in items)
                    spect.AddItem(item);
                foreach (var entry in ammo)
                    spect.SetAmmo(entry.Key, entry.Value);
                player.Role.Set(RoleTypeId.Spectator);
                EventHandlers.SendBroadcast(spect, Translations.SpectSwapSpectMessage);
            }),
            // 19: Teleports to a random Tesla gate if warhead is not detonated
            new CoinFlipEffect(Warhead.IsDetonated ? Translations.SmallDamageMessage : Translations.TeslaTpMessage, player =>
            {
                if (Warhead.IsDetonated)
                {
                    Sting(player, 15);
                    return;
                }

                player.DropHeldItem();
                var teslas = Exiled.API.Features.TeslaGate.List.ToList();
                if (teslas.Count > 0)
                    player.Teleport(teslas.RandomItem());
            }),
            // 20: Swaps inventory and ammo with another random player
            new CoinFlipEffect(Player.List.Where(x => !Config.InventorySwapIgnoredRoles.Contains(x.Role.Type)).Count(x => x.IsAlive) <= 1 ? Translations.InventorySwapOnePlayerMessage : Translations.InventorySwapMessage, player =>
            {
                List<Player> playerList = Player.List.Where(x => x != player && x.IsAlive && !Config.InventorySwapIgnoredRoles.Contains(x.Role.Type)).ToList();
                if (playerList.Count == 0)
                {
                    Sting(player, 50);
                    return;
                }

                var target = playerList.RandomItem();
                var items1 = player.Items.ToList();
                var items2 = target.Items.ToList();
                var ammo1 = player.Ammo.ToDictionary(entry => entry.Key.GetAmmoType(), entry => entry.Value);
                var ammo2 = target.Ammo.ToDictionary(entry => entry.Key.GetAmmoType(), entry => entry.Value);
                player.ClearInventory(false);
                target.ClearInventory(false);
                foreach (var item in items1)
                    target.AddItem(item);
                foreach (var item in items2)
                    player.AddItem(item);
                foreach (var entry in ammo2)
                    player.SetAmmo(entry.Key, entry.Value);
                foreach (var entry in ammo1)
                    target.SetAmmo(entry.Key, entry.Value);
                EventHandlers.SendBroadcast(target, Translations.InventorySwapMessage);
            }),
            // 21: Spawns a red candy or teleports the player to a random room based on warhead state.
            new CoinFlipEffect(Warhead.IsDetonated ? Translations.RandomTeleportWarheadDetonatedMessage : Translations.RandomTeleportMessage, player =>
            {
                if (Warhead.IsDetonated)
                {
                    Scp330 candy = (Scp330)Item.Create(ItemType.SCP330);
                    candy.AddCandy(InventorySystem.Items.Usables.Scp330.CandyKindID.Red);
                    candy.CreatePickup(player.Position);
                    return;
                }

                var rooms = Room.List.Where(room => Config.RoomsToTeleport != null && Config.RoomsToTeleport.Contains(room.Type)).ToList();
                if (rooms.Count > 0)
                    player.Teleport(rooms.RandomItem());
            }),
            // 22: Handcuffs the player and drops their items
            new CoinFlipEffect(Translations.HandcuffMessage, player =>
            {
                player.Handcuff();
                player.DropItems();
            }),
            // 23: Brainless: gives a cursed custom item (falls back to a small amount of damage).
            CursedItemPunishment(),
        };
    }
}
