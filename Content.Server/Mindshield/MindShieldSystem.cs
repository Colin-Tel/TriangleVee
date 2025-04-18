using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string MindShieldTag = "MindShield";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantImplantedEvent>(OnImplanted); // DeltaV - separate handlers for implanting and removal
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantRemovedEvent>(OnRemoved); // DeltaV - proper event
    }

    /// <summary>
    /// DeltaV: Adds components when implantedChecks if the implant was a mindshield or not
    /// </summary>
    public void OnImplanted(EntityUid uid, SubdermalImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (comp.AddedComponents is {} components && ev.Implanted is {} user)
            EntityManager.AddComponents(user, components);
    }

    /// <summary>
    /// DeltaV: Removes components when implanted.
    /// </summary>
    private void OnRemoved(Entity<SubdermalImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (ent.Comp.AddedComponents is {} components && args.Implanted is {} user)
            EntityManager.RemoveComponents(user, components);
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    public void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        if (HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted);
            QueueDel(implant);
            return;
        }

        if (_mindSystem.TryGetMind(implanted, out var mindId, out _) &&
            _roleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted)} was deconverted due to being implanted with a Mindshield.");
        }
    }

    /* DeltaV - unused
    private void OnImplantDraw(Entity<MindShieldImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RemComp<MindShieldComponent>(args.Container.Owner);
    }
    */
}

