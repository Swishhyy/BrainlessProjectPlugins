using System.Collections.Generic;
using System.IO;
using Brainless.Internal;
using Exiled.API.Features;

namespace Brainless.Items
{
    public sealed partial class Plugin
    {
        private static void LoadIndividualItems(Config config)
        {
            if (!config.IndividualItemFiles)
                return;
            var sections = new Dictionary<uint, ItemFiles.Section>();
            void Bind(uint id, object owner, string name) => sections[id] = ItemFiles.Bind(owner, name);
            Bind(7101, config.Tuning, "GasGrenade");
            Bind(7102, config.Tuning, "ClusterGrenade");
            Bind(7103, config.Tuning, "ContainmentFoam");
            Bind(7105, config.Tuning, "DoorBrace");
            Bind(7113, config.Tuning, "ImpactGrenade");
            Bind(7114, config.Sup, "AdrenalinePills");
            Bind(7115, config.Sup, "Anti096Pills");
            Bind(7116, config.Sup, "ExpiredPills");
            Bind(7117, config.Sup, "InvisibilityPills");
            Bind(7119, config.Sup, "HackingChip");
            Bind(7120, config.Sup, "ReinforcementRadio");
            Bind(7122, config.Sup, "VolatilePills");
            ItemFiles.Apply(Path.Combine(Paths.Configs, "ProjectBrainless", Server.Port.ToString(), "Items"), config.Items, i => i.Id, sections);
        }
    }
}
