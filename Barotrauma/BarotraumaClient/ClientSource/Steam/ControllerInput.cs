using Barotrauma.Extensions;
using Barotrauma.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Steamworks;
using Color = Microsoft.Xna.Framework.Color;
using SteamId = Barotrauma.Networking.SteamId;

namespace Barotrauma.Steam
{
    static partial class SteamManager
    {
        public static string LastGamepadText = string.Empty;
        public static void InitializeInput()
        {
            if (!File.Exists($"controller_config/game_actions_{AppID}.vdf"))
                DebugConsole.NewMessage("SteamInput: In-Game Actions File not found.", Color.Red);
                
            //SteamInput does not call Init-- this can be called via reflection to avoid recompiling from source
            //TODO: source changes are already made by barotrauma, this should just be made at source
            var steamInput = typeof(SteamClientClass<SteamInput>).GetRuntimeFields().FirstOrDefault()?.GetValue(null);
            if (steamInput != null)
            {
                steamInput.GetType().GetRuntimeMethods()
                    .Single(m => m.Name == "Init" && !m.GetParameters().Any())
                    .Invoke(steamInput, null);
            }
            
            Steamworks.SteamUtils.OnGamepadTextInputDismissed += UpdateGamepadText;
        }
        
        private static void UpdateGamepadText(bool submitted)
        {
            LastGamepadText = submitted ? Steamworks.SteamUtils.GetEnteredGamepadText() : string.Empty;
        }

        public static void TestMenuInteraction()
        {
            // var controller = SteamInput.Controllers.First();
            // controller.Id;
            // controller.ActionSet
            // controller.InputType
            // controller.ActivateLayer("layer");
            // controller.DeactivateLayer();
            // controller.ClearLayers();
            // controller.GetAnalogState();
            // controller.GetDigitalState();
            // controller.


            //Steamworks.SteamInput.GetDigitalActionGlyph()
            // if (Screen.Selected == GameMain.MainMenuScreen)
        }
    }
}
