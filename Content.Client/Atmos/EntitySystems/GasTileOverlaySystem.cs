using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly SpriteSystem _spriteSys = default!;

        private GasTileOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<GasOverlayUpdateEvent>(HandleGasOverlayUpdate);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            _overlay = new GasTileOverlay(this, _resourceCache, ProtoMan, _spriteSys);
            _overlayMan.AddOverlay(_overlay);
        }

        public override void Reset(RoundRestartCleanupEvent ev)
        {
            _overlay.TileData.Clear();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.RemoveOverlay<GasTileOverlay>();
            overlayManager.RemoveOverlay<FireTileOverlay>();
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            if (_tileData.ContainsKey(ev.GridId))
            {
                _tileData.Remove(ev.GridId);
            }
        }

        public bool HasData(EntityUid gridId)
        {
            return _tileData.ContainsKey(gridId);
        }

        public GasOverlayEnumerator GetOverlays(EntityUid gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return default;

            var chunkIndex = GetGasChunkIndices(indices);
            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return default;

            var overlays = chunk.GetData(indices);

            return new GasOverlayEnumerator(overlays, this);
        }

        public FireOverlayEnumerator GetFireOverlays(EntityUid gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return default;

            var chunkIndex = GetGasChunkIndices(indices);
            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return default;

            var overlays = chunk.GetData(indices);

            return new FireOverlayEnumerator(overlays, this);
        }

        private void HandleGasOverlayUpdate(GasOverlayUpdateEvent ev)
        {
            foreach (var (grid, removedIndicies) in ev.RemovedChunks)
            {
                if (!_overlay.TileData.TryGetValue(grid, out var chunks))
                    continue;

                foreach (var index in removedIndicies)
                {
                    chunks.Remove(index);
                }
            }

            foreach (var (grid, gridData) in ev.UpdatedChunks)
            {
                var chunks = _overlay.TileData.GetOrNew(grid);
                foreach (var chunkData in gridData)
                {
                    chunks[chunkData.Index] = chunkData;
                }
            }
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            _overlay.TileData.Remove(ev.EntityUid);
        }
    }
}
