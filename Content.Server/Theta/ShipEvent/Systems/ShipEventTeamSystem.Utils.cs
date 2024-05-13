//Because it's pain to work with 800+ line class

using System.Linq;
using System.Numerics;
using Content.Server.Access.Systems;
using Content.Server.Explosion.Components;
using Content.Shared.Chat;
using Content.Shared.Explosion;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem
{
    [Dependency] private readonly IdCardSystem _cardSys = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSys = default!;
    [Dependency] private readonly MobStateSystem _mobStateSys = default!;

    #region Teams

    private void Announce(string message, bool playSound = true)
    {
        _chatSys.DispatchGlobalAnnouncement(message, Loc.GetString("shipevent-announcement-title"), playSound);
    }

    /// <summary>
    /// Sends chat message to all team members
    /// </summary>
    /// <param name="team">team to which message should be send</param>
    /// <param name="message">message text</param>
    /// <param name="chatChannel">chat channel (local by default)</param>
    /// <param name="color">color of message (team's color by default)</param>
    public void TeamMessage(ShipEventTeam team, string message, ChatChannel chatChannel = ChatChannel.Local, Color? color = null)
    {
        color ??= team.Color;

        foreach (var session in GetTeamSessions(team))
        {
            _chatSys.SendSimpleMessage(message, session, chatChannel, color);
        }
    }

    private string GenerateTeamName()
    {
        _lastTeamNumber += 1;
        return $"Team №{_lastTeamNumber}";
    }

    public bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Length is > 25 or < 3)
            return false;

        return Teams.All(team => team.Name != name);
    }

    public List<ICommonSession> GetTeamSessions(ShipEventTeam team)
    {
        List<ICommonSession> sessions = new();
        foreach (string name in team.Members)
        {
            //if we can't resolve session by username user is disconnected
            if (_playerMan.TryGetSessionByUsername(name, out ICommonSession? session))
                sessions.Add(session);
        }

        return sessions;
    }

    public IEnumerable<ICommonSession> GetTeamLivingMembers(ShipEventTeam team)
    {
        return GetTeamSessions(team).Where(session => session.AttachedEntity != null &&
            TryComp<MobStateComponent>(session.AttachedEntity.Value, out var state) &&
            state.CurrentState != MobState.Dead);
    }

    #endregion

    //yes, RT does not have such a method
    private T Pick<T>(IEnumerable<T> enumerable)
    {
        return enumerable.ElementAt(_random.Next(0, enumerable.Count()));
    }

    private string GetName(EntityUid entity)
    {
        if (EntityManager.TryGetComponent(entity, out MetaDataComponent? metaComp))
            return metaComp.EntityName;

        return string.Empty;
    }

    private void SetName(EntityUid uid, string name)
    {
        _metaDataSys.SetEntityName(uid, name);
    }

    /// <summary>
    /// Sets character's entity name & id card name
    /// </summary>
    private void SetPlayerCharacterName(EntityUid uid, string name)
    {
        _metaDataSys.SetEntityName(uid, name);

        if (_cardSys.TryFindIdCard(uid, out var idCard))
            _cardSys.TryChangeFullName(idCard.Owner, name, idCard);

        _idSys.QueueIdentityUpdate(uid);
    }

    private HashSet<EntityUid> GetGridCompHolders<T>(EntityUid gridUid) where T : IComponent
    {
        HashSet<EntityUid> uids = new();
        var childEnum = Transform(gridUid).ChildEnumerator;

        while (childEnum.MoveNext(out EntityUid uid))
        {
            if (HasComp<T>(uid))
                uids.Add(uid);
        }

        return uids;
    }

    private HashSet<EntityUid> GetGridCompHolders<T>(IEnumerable<EntityUid> gridUids) where T : IComponent
    {
        HashSet<EntityUid> uids = new();

        foreach (EntityUid gridUid in gridUids)
        {
            uids.UnionWith(GetGridCompHolders<T>(gridUid));
        }

        return uids;
    }

    private void DetachEntityFromGrid(EntityUid uid)
    {
        TransformComponent form = Transform(uid);
        _formSys.SetParent(uid, form, _mapMan.GetMapEntityId(form.MapID));
    }

    private void DetachEnemiesFromGrid(EntityUid gridUid, ShipEventTeam team)
    {
        var childEnum = Transform(gridUid).ChildEnumerator;
        List<EntityUid> toDetach = new();
        while (childEnum.MoveNext(out EntityUid uid))
        {
            if (HasComp<GhostComponent>(uid) || TryComp<ShipEventTeamMarkerComponent>(uid, out var marker) && marker.Team != team)
                toDetach.Add(uid);
        }

        foreach (EntityUid uid in toDetach)
        {
            DetachEntityFromGrid(uid);
        }
    }

    private void DetachEnemiesFromGrid(IEnumerable<EntityUid> gridUids, ShipEventTeam team)
    {
        foreach (EntityUid gridUid in gridUids)
        {
            DetachEnemiesFromGrid(gridUid, team);
        }
    }

    private int GetProjectileDamage(EntityUid entity)
    {
        if (EntityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
        {
            if (meta.EntityPrototype == null)
                return 0;

            if (_projectileDamage.ContainsKey(meta.EntityPrototype.ID))
                return _projectileDamage[meta.EntityPrototype.ID];

            int damage = 0;

            if (EntityManager.TryGetComponent<ProjectileComponent>(entity, out var proj))
                damage += (int) proj.Damage.GetTotal();

            if (EntityManager.TryGetComponent<ExplosiveComponent>(entity, out var exp))
            {
                var damagePerIntensity = (int) _protMan.Index<ExplosionPrototype>(exp.ExplosionType).DamagePerIntensity.GetTotal();
                damage += (int) (exp.TotalIntensity * damagePerIntensity); //todo: this is inaccurate
            }

            _projectileDamage[meta.EntityPrototype.ID] = damage;

            return damage;
        }

        return 0;
    }

    private EntityUid? RandomPosEntSpawn(string prot, int attempts)
    {
        for (int c = 0; c < attempts; c++)
        {
            Box2 bounds = GetPlayAreaBounds();
            Vector2 pos = _random.NextVector2Box(bounds.BottomLeft.X, bounds.BottomLeft.Y, bounds.TopRight.X, bounds.TopRight.Y);

            if (_mapMan.TryFindGridAt(new(pos, TargetMap), out _, out _))
                continue;

            return SpawnAtPosition(prot, new(_mapMan.GetMapEntityId(TargetMap), pos));
        }

        return null;
    }
}

