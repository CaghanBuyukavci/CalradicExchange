using System;
using CalradicExchange.Behaviors;
using CalradicExchange.Core;
using CalradicExchange.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CalradicExchange
{
    /// <summary>Module entry point (SubModuleClassType in SubModule.xml).</summary>
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                ExchangeLog.Initialize(ModulePaths.LogsFolder);
                ExchangeLog.Info($"Calradic Exchange {CoreInfo.Version} loaded. Game {SafeGameVersion()}. Module root: {ModulePaths.ModuleRoot}");
            }
            catch (Exception ex)
            {
                // Logging itself failed; never take the game down because of it.
                Debug.Print("[CalradicExchange] Failed to initialize: " + ex);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            try
            {
                if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
                {
                    starter.AddBehavior(new ExchangeCampaignBehavior());
                    ExchangeLog.Info("Campaign started: exchange behavior registered.");
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("OnGameStart failed", ex);
            }
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            ExchangeLog.Info("Game ended.");
        }

        private static string SafeGameVersion()
        {
            try
            {
                return ApplicationVersion.FromParametersFile().ToString();
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}
