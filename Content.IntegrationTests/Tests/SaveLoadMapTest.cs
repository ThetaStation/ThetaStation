using System.Numerics;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class SaveLoadMapTest
    {
        [Test, Ignore("Too long for Theta Station")]
        public async Task SaveLoadMultiGridMap()
        {
            const string mapPath = @"/Maps/Test/TestMap.yml";

            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var mapManager = server.ResolveDependency<IMapManager>();
            var sEntities = server.ResolveDependency<IEntityManager>();
            var mapLoader = sEntities.System<MapLoaderSystem>();
            var xformSystem = sEntities.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
            var resManager = server.ResolveDependency<IResourceManager>();
            var cfg = server.ResolveDependency<IConfigurationManager>();
            Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

            await server.WaitAssertion(() =>
            {
                var dir = new ResPath(mapPath).Directory;
                resManager.UserData.CreateDir(dir);

                var mapId = mapManager.CreateMap();

                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = mapGrid.Owner;
                    xformSystem.SetWorldPosition(mapGridEnt, new Vector2(10, 10));
                    mapGrid.SetTile(new Vector2i(0, 0), new Tile(1, (TileRenderFlag) 1, 255));
                }
                {
                    var mapGrid = mapManager.CreateGrid(mapId);
                    var mapGridEnt = mapGrid.Owner;
                    xformSystem.SetWorldPosition(mapGridEnt, new Vector2(-8, -8));
                    mapGrid.SetTile(new Vector2i(0, 0), new Tile(2, (TileRenderFlag) 1, 254));
                }

                Assert.Multiple(() => mapLoader.SaveMap(mapId, mapPath));
                Assert.Multiple(() => mapManager.DeleteMap(mapId));
            });

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                Assert.That(mapLoader.TryLoad(new MapId(10), mapPath, out _));
            });

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                {
                    if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(10, 10), out var gridUid, out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(gridUid, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.Multiple(() =>
                    {
                        Assert.That(xformSystem.GetWorldPosition(gridXform), Is.EqualTo(new Vector2(10, 10)));
                        Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(1, (TileRenderFlag) 1, 255)));
                    });
                }
                {
                    if (!mapManager.TryFindGridAt(new MapId(10), new Vector2(-8, -8), out var gridUid, out var mapGrid) ||
                        !sEntities.TryGetComponent<TransformComponent>(gridUid, out var gridXform))
                    {
                        Assert.Fail();
                        return;
                    }

                    Assert.Multiple(() =>
                    {
                        Assert.That(xformSystem.GetWorldPosition(gridXform), Is.EqualTo(new Vector2(-8, -8)));
                        Assert.That(mapGrid.GetTileRef(new Vector2i(0, 0)).Tile, Is.EqualTo(new Tile(2, (TileRenderFlag) 1, 254)));
                    });
                }
            });

            await pair.CleanReturnAsync();
        }
    }
}
