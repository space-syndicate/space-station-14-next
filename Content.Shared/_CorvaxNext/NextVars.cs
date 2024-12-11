using Robust.Shared.Configuration;

namespace Content.Shared._CorvaxNext.NextVars;

/// <summary>
/// Corvax modules console variables
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class NextVars
{
    /// <summary>
    /// Offer item.
    /// </summary>
    public static readonly CVarDef<bool> OfferModeIndicatorsPointShow =
        CVarDef.Create("hud.offer_mode_indicators_point_show", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /*
    * AUTOVOTE SYSTEM
    */
    #region Autovote 

        /// <summary>
        /// Enables the automatic voting system.
        /// <summary>
        public static readonly CVarDef<bool> AutoVoteEnabled =
            CVarDef.Create("vote.autovote_enabled", false, CVar.SERVERONLY);
  
        /// <summary>
        /// Automatically starts a map vote when returning to the lobby.
        /// Requires auto voting to be enabled.
        /// <summary>
        public static readonly CVarDef<bool> MapAutoVoteEnabled =
            CVarDef.Create("vote.map_autovote_enabled", true, CVar.SERVERONLY);

        /// <summary>
        /// Automatically starts a gamemode vote when returning to the lobby.
        /// Requires auto voting to be enabled.
        /// <summary>
        public static readonly CVarDef<bool> PresetAutoVoteEnabled =
            CVarDef.Create("vote.preset_autovote_enabled", true, CVar.SERVERONLY);
    
    #endregion

    /// <summary>
    /// _CorvaxNext Surgery cvars
    /// </summary>
    #region Surgery

    public static readonly CVarDef<bool> CanOperateOnSelf =
        CVarDef.Create("surgery.can_operate_on_self", false, CVar.SERVERONLY);

    #endregion

    /*
     * _CorvaxNext Bind Standing and Laying System
     */

    public static readonly CVarDef<bool> AutoGetUp =
        CVarDef.Create("laying.auto_get_up", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    /// <summary>
    ///     When true, entities that fall to the ground will be able to crawl under tables and
    ///     plastic flaps, allowing them to take cover from gunshots.
    /// </summary>
    public static readonly CVarDef<bool> CrawlUnderTables =
        CVarDef.Create("laying.crawlundertables", false, CVar.REPLICATED);
}
