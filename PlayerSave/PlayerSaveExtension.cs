using System;
using System.IO;
using System.Reflection;
using System.Threading;
using fNbt;
using MiNET;
using MiNET.Effects;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Worlds;

namespace PlayerSave
{
    public static class PlayerSaveExtension
    {
        public static void Load(this Player player)
        {

            string path = Config.GetProperty("PluginDirectory", ".\\") + "\\PlayerSave\\players\\" + player.PlayerInfo.Username.ToLower() + ".dat";

            if (!File.Exists(path))
            {
                CreatePlayerData(player);
                return;
            }

            
            try
            {
                NbtFile file = new NbtFile();

                file.LoadFromFile(path, NbtCompression.ZLib, null);

                NbtCompound nbt = (NbtCompound)file.RootTag;

                NbtString levelName = nbt["Level"] as NbtString;

                Level level = player.GetServer().LevelManager.Levels.Find(obj =>
                {
                    return obj.LevelName == levelName.Value;
                });

                if (level != null)
                {
                    NbtList pos = nbt["Pos"] as NbtList;
                    NbtList rotation = nbt["Rotation"] as NbtList;
                    player.SpawnLevel(level, new PlayerLocation(pos[0].DoubleValue, pos[1].DoubleValue, pos[2].DoubleValue, 0, rotation[0].FloatValue, rotation[1].FloatValue));
                }

                player.HealthManager.Health = (int)nbt["Health"].FloatValue;

                //이펙트 보류
                //NbtList effects = nbt["ActiveEffects"] as NbtList;

                //foreach (NbtTag tag in effects)
                //{
                //    NbtCompound effectNbt = tag as NbtCompound;
                //}

                player.HungerManager.Hunger = nbt["foodLevel"].IntValue;
                player.HungerManager.Saturation = nbt["foodSaturationLevel"].FloatValue;
                player.HungerManager.Exhaustion = nbt["foodExhaustionLevel"].FloatValue;

                player.ExperienceManager.Experience = nbt["XpP"].FloatValue;
                player.ExperienceManager.ExperienceLevel = nbt["XpLevel"].IntValue;

                NbtList inventoryList = nbt["Inventory"] as NbtList;

                for (int i = 0; i < inventoryList.Count; i++)
                {
                    NbtCompound invNbt = inventoryList[i] as NbtCompound;
                    byte slot = invNbt["Slot"].ByteValue;
                    if (slot < 100)
                    {
                        if (player.Inventory.Slots.Count > i)
                            player.Inventory.SetInventorySlot(slot, ItemFactory.GetItem(invNbt["id"].ShortValue, invNbt["Damage"].ShortValue, invNbt["Count"].ByteValue));
                    }
                    else
                    {
                        switch (slot)
                        {
                            case 100:
                                player.Inventory.Helmet = ItemFactory.GetItem(invNbt["id"].ShortValue, invNbt["Damage"].ShortValue, invNbt["Count"].ByteValue);
                                break;
                            case 101:
                                player.Inventory.Chest = ItemFactory.GetItem(invNbt["id"].ShortValue, invNbt["Damage"].ShortValue, invNbt["Count"].ByteValue);
                                break;
                            case 102:
                                player.Inventory.Leggings = ItemFactory.GetItem(invNbt["id"].ShortValue, invNbt["Damage"].ShortValue, invNbt["Count"].ByteValue);
                                break;
                            case 103:
                                player.Inventory.Boots = ItemFactory.GetItem(invNbt["id"].ShortValue, invNbt["Damage"].ShortValue, invNbt["Count"].ByteValue);
                                break;
                        }
                    }
                }
                player.Inventory.InHandSlot = nbt["SelectedInventorySlot"].IntValue;

                player.SendPlayerInventory();
                player.SendArmorForPlayer();

                player.SetGameMode((GameMode)nbt["playerGameType"].IntValue);
            }
            catch (Exception e)
            {
                ConsoleColor col = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ForegroundColor = col;
            }
            
        }

        public static void CreatePlayerData(Player player, bool async = false)
        {

            NbtCompound namedTag = new NbtCompound("")
            {

                new NbtLong("firstPlayed", Microtime()),
                new NbtLong("lastPlayed", Microtime()),

                new NbtList("Pos", NbtTagType.Double)
                {
                    new NbtDouble(player.KnownPosition.X),
                    new NbtDouble(player.KnownPosition.Y),
                    new NbtDouble(player.KnownPosition.Z)
                },

                new NbtList("Motion", NbtTagType.Double)
                {
                    new NbtDouble(0.0),
                    new NbtDouble(0.0),
                    new NbtDouble(0.0)
                },

                new NbtList("Rotation", NbtTagType.Float)
                {
                    new NbtFloat(player.KnownPosition.Yaw),
                    new NbtFloat(player.KnownPosition.Pitch)
                },

                new NbtString("Level", player.Level.LevelName ?? ""),

                new NbtList("Inventory", NbtTagType.Compound),
                new NbtList("EnderChestInventory", NbtTagType.Compound),

                new NbtInt("SelectedInventorySlot", player.Inventory.InHandSlot),

                new NbtCompound("Achievements"),

                new NbtList("ActiveEffects", NbtTagType.Compound),

                new NbtInt("playerGameType", (int) player.GameMode),

                new NbtFloat("FallDistance", (float) 0.0),

                new NbtShort("Fire", 0),

                new NbtShort("Air", 300),

                new NbtByte("OnGround", 1),

                new NbtByte("Invulnerable", 0),

                new NbtString("NameTag", player.PlayerInfo.Username),

                new NbtFloat("Health", player.HealthManager.Health),

                new NbtFloat("XpP", player.ExperienceManager.Experience),

                new NbtInt("XpLevel", (int) player.ExperienceManager.ExperienceLevel),

                new NbtFloat("foodSaturationLevel", (float) player.HungerManager.Saturation),

                new NbtFloat("foodExhaustionLevel", (float) player.HungerManager.Exhaustion),

                new NbtInt("foodLevel", player.HungerManager.Hunger)

            };

            player.GetServer().SavePlayerData(player.PlayerInfo.Username, namedTag, async);

        }

        public static int Microtime()
        {
            TimeSpan span = DateTime.Now - new DateTime(1970, 1, 1);
            return (int) span.TotalSeconds;
        }

        public static void Save(this Player player, bool async = false)
        {
            NbtCompound namedTag = new NbtCompound("")
            {
                player.GetNbtPos(),
                player.GetNbtRotation(),

                player.GetNbtHealth(),
                //player.GetNbtEffects(),

                player.GetFoodNbt(),
                player.GetNbtFoodExhaustionLevel(),
                player.GetNbtFoodSaturationLevel(),

                player.GetNbtXpLevel(),
                player.GetNbtXpP(),

                player.GetNbtInventory(),
                player.GetNbtSelectedInventorySlot(),

                player.GetNbtLevel(),

                player.GetNbtPlayerGameType()
            };

            player.GetServer().SavePlayerData(player.PlayerInfo.Username, namedTag, async);
        }

        public static MiNetServer GetServer(this Player player)
        {
            PropertyInfo info = player.GetType().GetProperty("Server", BindingFlags.Instance | BindingFlags.NonPublic);
            return info.GetValue(player) as MiNetServer;
        }

        public static void SavePlayerData(this MiNetServer server, string name, NbtCompound nbtTag, bool async = false)
        {
            NbtFile nbt = new NbtFile(nbtTag);
            nbt.BigEndian = true;

            ParameterizedThreadStart threadStart = new ParameterizedThreadStart(obj =>
            {
                try
                {
                    string path = Config.GetProperty("PluginDirectory", ".\\") + "\\PlayerSave\\players\\";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    ((NbtFile)((object[])obj)[0]).SaveToFile(path + ((object[])obj)[1].ToString().ToLower() + ".dat", NbtCompression.ZLib);
                }
                catch (Exception e)
                {
                    ConsoleColor col = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ForegroundColor = col;
                }
            });
            Thread thread = new Thread(threadStart);

            if (async)
            {
                thread.Start(new object[] { nbt, name });
            }
            else
            {
                threadStart(new object[] { nbt, name });
            }
        }

        static NbtInt GetNbtPlayerGameType(this Player player)
        {
            return new NbtInt("playerGameType", (int)player.GameMode);
        }

        static NbtString GetNbtLevel(this Player player)
        {
            return new NbtString("Level", player.Level.LevelName ?? "");
        }

        public static NbtCompound NbtSerialize(this Item item, int slot = 1)
        {
            NbtCompound nbt = new NbtCompound
            {
                new NbtShort("id", item.Id),
                new NbtByte("Count", item.Count),
                new NbtShort("Damage", item.Metadata),

                new NbtByte("Slot", (byte)slot)
            };

            return nbt;
        }

        static NbtInt GetNbtSelectedInventorySlot(this Player player)
        {
            return new NbtInt("SelectedInventorySlot", player.Inventory.InHandSlot);
        }

        static NbtList GetNbtInventory(this Player player)
        {
            NbtTag[] tags = new NbtTag[104];

            for (int i = 0; i < player.Inventory.Slots.Count; i++)
            {
                tags[i] = player.Inventory.Slots[i].NbtSerialize(i);
            }

            for (int i = player.Inventory.Slots.Count; i < 100; i++)
            {
                tags[i] = new ItemAir().NbtSerialize(i);
            }

            tags[100] = player.Inventory.Helmet.NbtSerialize(100);
            tags[101] = player.Inventory.Chest.NbtSerialize(101);
            tags[102] = player.Inventory.Leggings.NbtSerialize(102);
            tags[103] = player.Inventory.Boots.NbtSerialize(103);


            NbtList nbt = new NbtList("Inventory", tags, NbtTagType.Compound);

            //@TODO EnderChest

            return nbt;
        }

        static NbtFloat GetNbtXpP(this Player player)
        {
            return new NbtFloat("XpP", player.ExperienceManager.Experience);
        }

        static NbtInt GetNbtXpLevel(this Player player)
        {
            return new NbtInt("XpLevel", (int)player.ExperienceManager.ExperienceLevel);
        }

        static NbtFloat GetNbtFoodSaturationLevel(this Player player)
        {
            return new NbtFloat("foodSaturationLevel", (float)player.HungerManager.Saturation);
        }

        static NbtFloat GetNbtFoodExhaustionLevel(this Player player)
        {
            return new NbtFloat("foodExhaustionLevel", (float)player.HungerManager.Exhaustion);
        }

        static NbtInt GetFoodNbt(this Player player)
        {
            return new NbtInt("foodLevel", player.HungerManager.Hunger);
        }

        /*static NbtList GetNbtEffects(this Player player)
        {
            NbtList nbt = new NbtList("ActiveEffects", NbtTagType.Compound);
            foreach (Effect effect in player.Effects.Values)
            {
                NbtCompound nbtCompound = new NbtCompound
                {
                    new NbtByte("Id", (byte)effect.EffectId),
                    new NbtInt("Duration", effect.Duration),
                    new NbtByte("ShowParticles", (byte)(effect.Particles ? 1 : 0))
                };
                nbt.Add(nbtCompound);
            }

            return nbt;
        }*/

        static NbtList GetNbtPos(this Player player)
        {
            NbtList nbt = new NbtList("Pos", NbtTagType.Double)
            {
                new NbtDouble(player.KnownPosition.X),
                new NbtDouble(player.KnownPosition.Y),
                new NbtDouble(player.KnownPosition.Z)
            };
            return nbt;
        }

        static NbtList GetNbtRotation(this Player player)
        {
            NbtList nbt = new NbtList("Rotation", NbtTagType.Float)
            {
                new NbtFloat(player.KnownPosition.Yaw),
                new NbtFloat(player.KnownPosition.Pitch)
            };
            return nbt;
        }

        static NbtFloat GetNbtHealth(this Player player)
        {
            return new NbtFloat("Health", player.HealthManager.Health);
        }

    }
}
