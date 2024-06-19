using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;
using Vintagestory.Server;
using HarmonyLib;
using Vintagestory.GameContent;
using System.Net;
using System.Text.RegularExpressions;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AutomaticChiselling
{

    internal static class Patchs
    {
        private static Harmony harmony;

        public static void PatchAll()
        {
            harmony = new Harmony("AutomaticChiselling");
            PachingAddModel();
        }

        private static void PachingAddModel()
        {
            var originalAddModel = typeof(MeshDataPoolManager).GetMethod("AddModel", BindingFlags.Public | BindingFlags.Instance);
            var prefixAddModel = typeof(Patchs).GetMethod("AddModelPrefix", BindingFlags.Public | BindingFlags.Static);
            harmony.Patch(originalAddModel, new HarmonyMethod(prefixAddModel));
        }

        public static bool AddModelPrefix(MeshDataPoolManager __instance, MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere, ref ModelDataPoolLocation __result)
        {
            Type type = typeof(MeshDataPoolManager);

            FieldInfo poolsFieldInfo = type.GetField("pools", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo capiFieldInfo = type.GetField("capi", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo defaultIndexPoolSizeFieldInfo = type.GetField("defaultIndexPoolSize", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo defaultVertexPoolSizeFieldInfo = type.GetField("defaultVertexPoolSize", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo maxPartsPerPoolFieldInfo = type.GetField("maxPartsPerPool", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo customFloatsFieldInfo = type.GetField("customFloats", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo customShortsFieldInfo = type.GetField("customShorts", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo customBytesFieldInfo = type.GetField("customBytes", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo customIntsFieldInfo = type.GetField("customInts", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo masterPoolFieldInfo = type.GetField("masterPool", BindingFlags.NonPublic | BindingFlags.Instance);

            ModelDataPoolLocation location = null;
            for (int i = 0; i < ((List<MeshDataPool>)poolsFieldInfo.GetValue(__instance)).Count; i++)
            {
                location = ((List<MeshDataPool>)poolsFieldInfo.GetValue(__instance))[i].TryAdd((ICoreClientAPI)capiFieldInfo.GetValue(__instance), modeldata, modelOrigin, dimension, frustumCullSphere);
                if (location != null)
                {
                    break;
                }
            }
            if (location == null)
            {
                int vertexSize = Math.Max(modeldata.VerticesCount + 1, (int)defaultVertexPoolSizeFieldInfo.GetValue(__instance));
                int indexSize = Math.Max(modeldata.IndicesCount + 1, (int)defaultIndexPoolSizeFieldInfo.GetValue(__instance));
                if (vertexSize > (int)defaultIndexPoolSizeFieldInfo.GetValue(__instance))
                {
                    ((ICoreClientAPI)capiFieldInfo.GetValue(__instance)).World.Logger.Warning("Chunk (or some other mesh source at origin: {0}) exceeds default geometric complexity maximum of {1} vertices and {2} indices. You must be loading some very complex objects (#v = {3}, #i = {4}). Adjusted Pool size accordingly.", new object[]
                    {
                        modelOrigin,
                        (int)defaultVertexPoolSizeFieldInfo.GetValue(__instance),
                        (int)defaultIndexPoolSizeFieldInfo.GetValue(__instance),
                        modeldata.VerticesCount,
                        modeldata.IndicesCount
                    });
                }
                MeshDataPool pool = MeshDataPool.AllocateNewPool((ICoreClientAPI)capiFieldInfo.GetValue(__instance), vertexSize * 2, indexSize * 2, (int)maxPartsPerPoolFieldInfo.GetValue(__instance), (CustomMeshDataPartFloat)customFloatsFieldInfo.GetValue(__instance), (CustomMeshDataPartShort)customShortsFieldInfo.GetValue(__instance), (CustomMeshDataPartByte)customBytesFieldInfo.GetValue(__instance), (CustomMeshDataPartInt)customIntsFieldInfo.GetValue(__instance));

                FieldInfo poolOriginFieldInfo = typeof(MeshDataPool).GetField("poolOrigin", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo dimensionIdFieldInfo = typeof(MeshDataPool).GetField("dimensionId", BindingFlags.NonPublic | BindingFlags.Instance);

                poolOriginFieldInfo.SetValue(pool, modelOrigin);
                dimensionIdFieldInfo.SetValue(pool, dimension);

                ((MeshDataPoolMasterManager)masterPoolFieldInfo.GetValue(__instance)).AddModelDataPool(pool);
                ((List<MeshDataPool>)poolsFieldInfo.GetValue(__instance)).Add(pool);
                location = pool.TryAdd((ICoreClientAPI)capiFieldInfo.GetValue(__instance), modeldata, modelOrigin, dimension, frustumCullSphere);
            }
            if (location == null)
            {
                ((ICoreClientAPI)capiFieldInfo.GetValue(__instance)).World.Logger.Fatal("Can't add modeldata (probably a tesselated chunk @{0}) to the model data pool list, it exceeds the size of a single empty pool of {1} vertices and {2} indices. You must be loading some very complex objects (#v = {3}, #i = {4}). Try increasing MaxVertexSize and MaxIndexSize. The whole chunk will be invisible.", new object[]
                {
                    modelOrigin,
                    (int)defaultVertexPoolSizeFieldInfo.GetValue(__instance),
                    (int)defaultIndexPoolSizeFieldInfo.GetValue(__instance),
                    modeldata.VerticesCount,
                    modeldata.IndicesCount
                });
            }
            __result = location;
            return false;
        }

    }
}
