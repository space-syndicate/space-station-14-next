using Content.Server.Radio.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Telephone;
using Content.Shared._CorvaxNext.Silicons.Borgs;
using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Telephone;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._CorvaxNext.Silicons.Borgs
{
    public sealed class AiRemoteControlSystem : SharedAiRemoteControlSystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SiliconLawSystem _lawSystem = default!;
        [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
        [Dependency] private readonly TelephoneSystem _telephoneSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AiRemoteControllerComponent, ReturnMindIntoAiEvent>(OnReturnMindIntoAi);
            SubscribeLocalEvent<AiRemoteControllerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<AiRemoteControllerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<AiRemoteControllerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
            SubscribeLocalEvent<AiRemoteControllerComponent, MobStateChangedEvent>(OnMobState);
            SubscribeLocalEvent<StationAiHeldComponent, AiRemoteControllerComponent.RemoteDeviceActionMessage>(OnUiRemoteAction);

            SubscribeLocalEvent<StationAiHeldComponent, ToggleRemoteDevicesScreenEvent>(OnToggleRemoteDevicesScreen);
        }

        private void OnMapInit(Entity<AiRemoteControllerComponent> entity, ref MapInitEvent args)
        {
            var visionComp = AddComp<StationAiVisionComponent>(entity);

            if (HasComp<IonStormTargetComponent>(entity))
                entity.Comp.HadIonStormTargetBefore = true;

            RemComp<IonStormTargetComponent>(entity);
            EntityUid? actionEnt = null;

            _actions.AddAction(entity, ref actionEnt, entity.Comp.BackToAiAction);

            if (actionEnt != null)
                entity.Comp.BackToAiActionEntity = actionEnt.Value;
        }

        private void OnShutdown(Entity<AiRemoteControllerComponent> entity, ref ComponentShutdown args)
        {
            _actions.RemoveAction(entity, entity.Comp.BackToAiActionEntity);

            RemComp<StationAiVisionComponent>(entity);

            var backArgs = new ReturnMindIntoAiEvent();
            backArgs.Performer = entity;

            if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter) && entity.Comp.PreviouslyTransmitterChannels != null)
                transmitter.Channels = [.. entity.Comp.PreviouslyTransmitterChannels];

            if (TryComp(entity, out ActiveRadioComponent? activeRadio) && entity.Comp.PreviouslyActiveRadioChannels != null)
                activeRadio.Channels = [.. entity.Comp.PreviouslyActiveRadioChannels];

            if (entity.Comp.HadIonStormTargetBefore)
                EnsureComp<IonStormTargetComponent>(entity);

            ReturnMindIntoAi(entity);
        }

        private void OnMobState(Entity<AiRemoteControllerComponent> ent, ref MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
                ToggleDeviceEnabled(ent, false);
            else if (args.OldMobState == MobState.Dead)
                ToggleDeviceEnabled(ent, true);
        }

        private void OnGetVerbs(Entity<AiRemoteControllerComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
        {
            var user = args.User;

            if (!TryComp<StationAiHeldComponent>(user, out var stationAiHeldComp))
                return;

            var verb = new AlternativeVerb
            {
                Text = Loc.GetString("ai-remote-control"),
                Act = () =>
                {
                    AiTakeControl(user, entity);
                }
            };
            args.Verbs.Add(verb);
        }

        private void OnReturnMindIntoAi(Entity<AiRemoteControllerComponent> entity, ref ReturnMindIntoAiEvent args)
        {
            ReturnMindIntoAi(entity);
        }

        public void AiTakeControl(EntityUid ai, EntityUid entity)
        {
            if (!_mind.TryGetMind(ai, out var mindId, out var mind))
                return;

            if (!TryComp<StationAiHeldComponent>(ai, out var stationAiHeldComp))
                return;

            if (!TryComp<AiRemoteControllerComponent>(entity, out var aiRemoteComp))
                return;

            if (!aiRemoteComp.Enabled)
                return;

            if (!_stationAiSystem.TryGetCore(ai, out var stationAiCore))
                return;

            if (!TryComp<TelephoneComponent>(stationAiCore, out var stationAiCoreTelephone))
                return;

            RemComp<IonStormTargetComponent>(entity);

            _telephoneSystem.EndTelephoneCalls((stationAiCore, stationAiCoreTelephone));

            if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter))
            {
                aiRemoteComp.PreviouslyTransmitterChannels = [.. transmitter.Channels];

                if (TryComp(ai, out IntrinsicRadioTransmitterComponent? stationAiTransmitter))
                    transmitter.Channels = [.. stationAiTransmitter.Channels];
            }

            if (TryComp(entity, out ActiveRadioComponent? activeRadio))
            {
                aiRemoteComp.PreviouslyActiveRadioChannels = [.. activeRadio.Channels];

                if (TryComp(ai, out ActiveRadioComponent? stationAiActiveRadio))
                    activeRadio.Channels = [.. stationAiActiveRadio.Channels];
            }

            _mind.ControlMob(ai, entity);
            aiRemoteComp.AiHolder = ai;
            aiRemoteComp.LinkedMind = mindId;

            stationAiHeldComp.CurrentConnectedEntity = entity;

            _stationAiSystem.SwitchRemoteEntityMode(stationAiCore, false);

            RewriteLaws(ai, entity);
        }

        private void OnToggleRemoteDevicesScreen(EntityUid uid, StationAiHeldComponent component, ToggleRemoteDevicesScreenEvent args)
        {
            if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
                return;
            args.Handled = true;

            _userInterface.TryToggleUi(uid, RemoteDeviceUiKey.Key, actor.PlayerSession);

            var query = EntityManager.EntityQueryEnumerator<AiRemoteControllerComponent>();

            var remoteDevices = new List<RemoteDevicesData>();

            while (query.MoveNext(out var queryUid, out var comp))
            {
                var data = new RemoteDevicesData
                {
                    NetEntityUid = GetNetEntity(queryUid),
                    DisplayName = Comp<MetaDataComponent>(queryUid).EntityName
                };

                remoteDevices.Add(data);
            }

            var state = new RemoteDevicesBuiState(remoteDevices);

            _userInterface.SetUiState(uid, RemoteDeviceUiKey.Key, state);
        }

        private void OnUiRemoteAction(EntityUid uid, StationAiHeldComponent component, AiRemoteControllerComponent.RemoteDeviceActionMessage msg)
        {
            if (msg.RemoteAction == null)
                return;

            var target = GetEntity(msg.RemoteAction?.Target);

            if (!TryComp<AiRemoteControllerComponent>(target, out var controller))
                return;

            if (controller.Enabled == false)
                return;

            if (msg.RemoteAction?.ActionType == RemoteDeviceActionEvent.RemoteDeviceActionType.MoveToDevice)
            {
                if (!_stationAiSystem.TryGetCore(uid, out var stationAiCore) || stationAiCore.Comp?.RemoteEntity == null)
                    return;
                _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(target.Value).Coordinates);
            }

            if (msg.RemoteAction?.ActionType == RemoteDeviceActionEvent.RemoteDeviceActionType.TakeControl)
            {
                AiTakeControl(uid, target.Value);
            }
        }

        private void RewriteLaws(EntityUid from, EntityUid to)
        {
            if (!TryComp<SiliconLawProviderComponent>(from, out var fromLawsComp))
                return;

            if (!TryComp<SiliconLawProviderComponent>(to, out var toLawsComp))
                return;

            if (fromLawsComp.Lawset == null)
                return;

            var fromLaws = _lawSystem.GetLaws(from);

            _lawSystem.SetLawsSilent(fromLaws.Laws, to);
        }
    }
}
