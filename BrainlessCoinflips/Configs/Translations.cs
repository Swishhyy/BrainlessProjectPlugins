using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Brainless.Coinflips.Configs
{
    public class Translations : ITranslation
    {
        [Description("This is added to the effect message if the coin breaks.")]
        public string CoinBreaksMessage { get; set; } = "\nAlso your coin was used too much and it broke down.";

        [Description("The broadcast message when a coin is registered with no uses.")]
        public string CoinNoUsesMessage { get; set; } = "Your coin had no uses to begin with!";
        public List<string> HintMessages { get; set; } = new()
        {
            "Your coin landed on tails.",
            "Your coin landed on heads."
        };

        [Description("Here you can set the message for each of these good coin effects.")]
        public string TossOnCooldownMessage { get; set; } = "You can't throw the coin yet.";
        public string RedCardMessage { get; set; } = "You acquired a Facility Manager keycard!";
        public string ContainmentEngineerCardMessage { get; set; } = "You acquired a Containment Engineer keycard!";
        public string MediKitMessage { get; set; } = "You received a Medical Kit!";
        public string TpToEscapeMessage { get; set; } = "You can now escape! That's what you wanted right?";
        public string MagicHealMessage { get; set; } = "You've been magically healed!";
        public string HealthIncreaseMessage { get; set; } = "You received 10% more hp!";
        public string NeatHatMessage { get; set; } = "You got a neat hat!";
        public string RandomGoodEffectMessage { get; set; } = "You got a random effect.";
        public string OneAmmoLogicerMessage { get; set; } = "You got gun.";
        public string LightbulbMessage { get; set; } = "You got a shiny lightbulb!";
        public string PinkCandyMessage { get; set; } = "You got a pretty candy!";
        public string BadRevoMessage { get; set; } = "What is this abomination!?";
        public string EmptyHidMessage { get; set; } = "DID YOU JUST GET A MICRO HID!?";
        public string ForceRespawnMessage { get; set; } = "Someone respawned... probably.";
        public string SizeChangeMessage { get; set; } = "You got gnomed.";
        public string RandomItemMessage { get; set; } = "You got a random item!";
        public string CustomItemMessage { get; set; } = "You got a {item}!";
        public string CustomRoleMessage { get; set; } = "The coin made you a {role}!";
        public string SpeedBoostMessage { get; set; } = "You feel fast!";
        public string ShieldUpMessage { get; set; } = "A shield wraps around you!";
        public string ToughSkinMessage { get; set; } = "Your skin feels tougher.";
        public string SecondWindMessage { get; set; } = "Second wind! Run forever for a bit.";
        public string AmmoRefillMessage { get; set; } = "Your ammo has been refilled!";
        public string KeycardUpgradeMessage { get; set; } = "Your keycard got an upgrade!";
        public string RandomScpItemMessage { get; set; } = "An SCP item appeared at your feet!";
        public string LuckyCoinMessage { get; set; } = "Lucky coin! It gained {flips} extra flips.";
        public string SecondChanceMessage { get; set; } = "Second chance: the next hit that would kill you won't.";
        public string FullHealMessage { get; set; } = "You feel brand new!";
        public string FrozenMessage { get; set; } = "Your legs stopped working!";
        public string SlowedMessage { get; set; } = "You feel sluggish...";
        public string ExhaustedMessage { get; set; } = "You're out of breath.";
        public string SinkholeMessage { get; set; } = "You stepped in something sticky.";
        public string ButterfingersMessage { get; set; } = "Butterfingers! You dropped everything.";
        public string DropHeldItemMessage { get; set; } = "Oops, you dropped that.";
        public string LoseAmmoMessage { get; set; } = "Your spare ammo vanished.";
        public string EmptyMagazineMessage { get; set; } = "Your magazine is suddenly empty.";
        public string BlindedMessage { get; set; } = "Everything went dark!";
        public string DeafenedMessage { get; set; } = "Your ears are ringing.";
        public string ConcussedMessage { get; set; } = "Your head is spinning...";
        public string FlashedMessage { get; set; } = "Blinding light!";
        public string LocationLeakBroadcast { get; set; } = "{player} just flipped tails in {zone}!";
        public string LocationLeakMessage { get; set; } = "The coin told everyone where you are.";
        public string CassieCalloutMessage { get; set; } = "CASSIE noticed you.";
        public string LockedInMessage { get; set; } = "The doors around you locked!";
        public string RoomBlackoutMessage { get; set; } = "The lights in here just died.";
        public string ItemWipeMessage { get; set; } = "The coin ate ALL of your items. Ouch.";
        public string DropKeycardsMessage { get; set; } = "Your keycards slipped out of your pocket.";
        public string DropArmorMessage { get; set; } = "Your armor fell off!";
        public string CursedMaxHpMessage { get; set; } = "Cursed! Your max health dropped.";
        public string BleedingMessage { get; set; } = "You're bleeding!";
        public string HypothermiaMessage { get; set; } = "You're freezing!";
        public string StaminaDrainedMessage { get; set; } = "Your stamina is gone.";
        public string PoisonedMessage { get; set; } = "You feel sick...";
        public string HuntedMessage { get; set; } = "The SCPs know where you are...";
        public string HuntedHint { get; set; } = "{player} is in {zone}.";
        public string BackToSpawnMessage { get; set; } = "Back to where you started!";
        public string JunkSwapMessage { get; set; } = "One of your items turned into junk.";
        public string TripMessage { get; set; } = "You tripped!";
        public string WeakArmsMessage { get; set; } = "Your arms feel weak.";
        public string GlassJawMessage { get; set; } = "You feel fragile...";
        public string UnluckyCoinMessage { get; set; } = "The coin crumbled in your hand.";
        public string ArmorUpgradeMessage { get; set; } = "Your armor got an upgrade!";
        public string GrenadePackMessage { get; set; } = "Grenades! Use them wisely.";
        public string ExtraCoinMessage { get; set; } = "Another coin! Keep gambling.";
        public string LoadedUpMessage { get; set; } = "Locked and loaded!";
        public string RegenerationMessage { get; set; } = "You feel your wounds closing...";
        public string SilentStepsMessage { get; set; } = "Your footsteps went silent.";
        public string CleanseMessage { get; set; } = "All your ailments are gone!";
        public string MedicAuraMessage { get; set; } = "A healing wave washes over you and your team!";
        public string RallyMessage { get; set; } = "You rallied to a teammate!";
        public string ReinforcementMessage { get; set; } = "Backup has arrived: {player} joins you!";
        public string ReinforcementJoinMessage { get; set; } = "The coin brought you back to help {player}!";
        public string JackpotMessage { get; set; } = "JACKPOT! ";
        public string MonkeysPawPrefix { get; set; } = "Monkey's Paw: ";
        public string PawCrumblesMessage { get; set; } = "\nThe paw crumbles to dust.";
        public string FastThenWindedMessage { get; set; } = "You wished for speed. Enjoy it while it lasts...";
        public string WindedMessage { get; set; } = "The Monkey's Paw wants its speed back: your stamina recovers slowly now.";
        public string TankButHeavyMessage { get; set; } = "You wished to be tougher. You are... and heavier.";
        public string UnkillableMessage { get; set; } = "Nothing can kill you for a few seconds. Make them count.";
        public string UnkillableEndMessage { get; set; } = "The protection is gone... and so is your health.";
        public string SilentButDeafMessage { get; set; } = "Your steps are silent. So is everything else.";
        public string StrongButFragileMessage { get; set; } = "You hit harder. So does everyone else.";
        public string EndlessAmmoMessage { get; set; } = "Your gun will never run dry... for now.";
        public string NoAmmoMessage { get; set; } = "The Monkey's Paw took your spare ammo.";
        public string FullHealThenBlindMessage { get; set; } = "Fully healed! But at what cost...";
        public string GhostMessage { get; set; } = "You vanish from sight...";
        public string GhostEndMessage { get; set; } = "You reappear, and the SCPs know where.";
        public string GhostHint { get; set; } = "{player} reappeared in {zone}.";
        public string RichesButFrailMessage { get; set; } = "Riches at your feet! Your body feels weaker.";
        public string FreeExitMessage { get; set; } = "A free exit! Your belongings stayed behind.";
        public string LuckyThenCursedMessage { get; set; } = "Your next {flips} flips are lucky. The {flips} after that are not.";
        public string CursedItemMessage { get; set; } = "The coin cursed you with {item}...";

        [Description("Here you can set the message for each of these bad coin effects.")]
        public string HpReductionMessage { get; set; } = "Your hp got reduced by 30%.";
        public string TpToClassDCellsMessage { get; set; } = "You got teleported to Class D cells.";
        public string TpToClassDCellsAfterWarheadMessage { get; set; } = "You were teleported into a radioactive zone.";
        public string RandomBadEffectMessage { get; set; } = "You got a random effect.";
        public string WarheadStopMessage { get; set; } = "The warhead has been stopped.";
        public string WarheadStartMessage { get; set; } = "The warhead has been started.";
        public string LightsOutMessage { get; set; } = "Lights out.";
        public string LiveGrenadeMessage { get; set; } = "Watch your head!";
        public string TrollFlashMessage { get; set; } = "You heard something?";
        public string TpToRandomScpMessage { get; set; } = "You were teleported to an SCP.";
        public string SmallDamageMessage { get; set; } = "You've lost 15hp.";
        public string HugeDamageMessage { get; set; } = "You've lost a lot of hp";
        public string PrimedVaseMessage { get; set; } = "Your grandma paid you a visit!";
        public string ShitPantsMessage { get; set; } = "You just shit your pants.";
        public string FakeScpKillMessage { get; set; } = "Did you just kill an SCP?!";
        public string TurnIntoScpMessage { get; set; } = "Get SCP-fied LOL!";
        public string InventoryResetMessage { get; set; } = "You lost your stuff.";
        public string ClassSwapMessage { get; set; } = "That's what I call an UNO reverse card!";
        public string InstantExplosionMessage { get; set; } = "You got smoked.";
        public string PlayerSwapMessage { get; set; } = "Your inventory was swapped with a random player.";
        public string PlayerSwapIfOneAliveMessage { get; set; } = "You were supposed to switch places with someone but no one else is alive!";
        public string KickMessage { get; set; } = "Bye!";
        public string SpectSwapPlayerMessage { get; set; } = "You just made someone's round better!";
        public string SpectSwapSpectMessage { get; set; } = "You were chosen as a random spectator to replace this player!";
        public string SpectSwapNoSpectsMessage { get; set; } = "You got lucky cause there are no spectators to take your place.";
        public string TeslaTpMessage { get; set; } = "So you're a fan of electricity?";
        public string TeslaTpAfterWarheadMessage { get; set; } = "You were teleported into a radioactive zone.";

        [Description("This message will be broadcast to both players.")]
        public string InventorySwapMessage { get; set; } = "Your inventory was swapped with a random player.";
        public string InventorySwapOnePlayerMessage { get; set; } = "You can't swap with anyone so you're losing health instead.";
        public string RandomTeleportMessage { get; set; } = "You were randomly teleported.";
        public string RandomTeleportWarheadDetonatedMessage { get; set; } = "Warhead is detonated so you only got a candy.";
        public string HandcuffMessage { get; set; } = "You were arrested for uhh commiting war crimes... or something.";
    }
}
