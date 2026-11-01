using System;
using System.Reflection;
using HarmonyLib;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Scp914;
using Scp914.Processors;

namespace Brainless.Internal
{
    internal static class Native914
    {
        private static readonly MethodInfo processPickup = Required(typeof(Scp914Upgrader), "ProcessPickup", typeof(ItemPickupBase), typeof(bool), typeof(Scp914KnobSetting));
        private static readonly MethodInfo getProcessor = Required(typeof(Scp914Upgrader), "TryGetProcessor", typeof(ItemType), typeof(Scp914ItemProcessor).MakeByRefType());
        private static readonly MethodInfo processResult = Required(typeof(Scp914ItemProcessor), "ProcessResultingItem", typeof(ItemBase), typeof(Scp914Result), typeof(Scp914KnobSetting));
        private static MethodInfo Required(Type type, string name, params Type[] parameters) => AccessTools.Method(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);
        internal static void ProcessPickup(ItemPickupBase pickup, bool upgrade, Scp914KnobSetting setting) => processPickup.Invoke(null, new object[] { pickup, upgrade, setting });
        internal static void ProcessInventory(ItemBase item, Scp914KnobSetting setting)
        {
            object[] arguments =
            {
                item.ItemTypeId,
                null
            };
            if (!(bool)getProcessor.Invoke(null, arguments))
                return;
            var result = ((Scp914ItemProcessor)arguments[1]).UpgradeInventoryItem(setting, item);
            processResult.Invoke(null, new object[] { item, result, setting });
        }
    }
}
