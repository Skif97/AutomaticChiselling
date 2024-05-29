using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VoxReader.Interfaces;

namespace AutomaticChiselling
{
    using DictionaryVoxelsInBlock = Dictionary<BlockPos, Dictionary<Vec3i, Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>>>;
    public class MyVoxFormat
    {
        DictionaryVoxelsInBlock VoxelsInBlocks;

        Vec3i VoxelsOffset = new Vec3i(0,0,0);
        Vec3i BlocksOffset = new Vec3i(0,0,0);
        Vec3i MinPos;
        Vec3i MaxPos;
        string Allign = "CENTER";
        Vec3i BlockCorrection = new Vec3i(0,0,0);

        string voxFileName;
        IVoxFile voxFile;
        List<Vec3i> rawVoxels;


        public MyVoxFormat(string filename) 
        {
            if (!LoadFromFile(filename)) 
            {
                return;
            }
            voxFileName = filename;
            LoadRawVoxels();
            ApplyAllignModel();
            ConvertToMyFormat();
            FindMinMaxPos();

        }

        private bool LoadFromFile(string filename) 
        {
            string basePath = Path.Combine(GamePaths.DataPath, "worldedit");
            basePath = Path.Combine(basePath, filename) + ".vox";
            if (!File.Exists(basePath))
            {
                return false;
            }
           
            try
            {
                voxFile = VoxReader.VoxReader.Read(basePath);
            }
            catch (FileNotFoundException e)
            {
                return false;
            }
            return true;
        }

        private void LoadRawVoxels()
        {
            rawVoxels = new List<Vec3i>();

            foreach (var model in voxFile.Models)
            {
                Vec3i modelPos = new Vec3i(model.Position.X, model.Position.Z, -model.Position.Y);

                foreach (var voxelInModel in model.Voxels)
                {
                    Vec3i voxel = new Vec3i(voxelInModel.Position.X, voxelInModel.Position.Z, -voxelInModel.Position.Y);
                    voxel += modelPos;
                    rawVoxels.Add(voxel);
                }
            }
        }

        private void ApplyAllignModel() 
        {
            if (rawVoxels.Count == 0)
            {
                return;
            }
            //ищем минимумы и максимумы для дальнейшего выравнивания
            int minX = rawVoxels[0].X;
            int minY = rawVoxels[0].Y;
            int minZ = rawVoxels[0].Z;
            int maxX = rawVoxels[0].X;
            int maxY = rawVoxels[0].Y;
            int maxZ = rawVoxels[0].Z;
            
            foreach (var voxel in rawVoxels)
            {
                if (voxel.X < minX) { minX = voxel.X; }
                if (voxel.Y < minY) { minY = voxel.Y; }
                if (voxel.Z < minZ) { minZ = voxel.Z; }
                if (voxel.X > maxX) { maxX = voxel.X; }
                if (voxel.Y > maxY) { maxY = voxel.Y; }
                if (voxel.Z > maxZ) { maxZ = voxel.Z; }
            }

            foreach (var voxel in rawVoxels)
            {
                voxel.Add(minX * -1, minY * -1, minZ * -1);
            }

            int sizeX = maxX - minX + 1;
            int sizeY = maxY - minY + 1;
            int sizeZ = maxZ - minZ + 1;

            int BlockSizeX = (int)Math.Ceiling(sizeX / 16f);
           // int BlockSizeY = (int)Math.Ceiling(sizeY / 16f);
            int BlockSizeZ = (int)Math.Ceiling(sizeZ / 16f);

            int correctX = 0;
            int correctY = 0;
            int correctZ = 0;

            switch (Allign) //сделать енумератор
            {
                case "SE":
                    break;

                case "NE":
                    correctX = (BlockSizeX * 16 - sizeX);
                    BlockCorrection = new Vec3i(BlockSizeX, 0, 0);
                    break;

                case "SW":
                    correctZ = (BlockSizeZ * 16 - sizeZ);
                    BlockCorrection = new Vec3i(0, 0, BlockSizeZ);
                    break;
                case "NW":
                    correctX = (BlockSizeX * 16 - sizeX);
                    correctZ = (BlockSizeZ * 16 - sizeZ);
                    BlockCorrection = new Vec3i(BlockSizeX, 0, BlockSizeZ);
                    break;

                case "CENTER":
                    correctX = (int)Math.Truncate((BlockSizeX * 16 - sizeX) / 2f);
                    correctZ = (int)Math.Truncate((BlockSizeZ * 16 - sizeZ) / 2f);
                    BlockCorrection = new Vec3i((int)Math.Truncate(BlockSizeX / 2f), 0, (int)Math.Truncate(BlockSizeZ / 2f));
                    break;

                default: break;
            }

            //делаем корректировку модели 
            foreach (var voxel in rawVoxels)
            {
                voxel.Add(correctX, correctY, correctZ);
            }
        }

        private void ApplyVoxelsOffset() 
        {
            if(VoxelsOffset != new Vec3i(0, 0, 0)) 
            {
                foreach (var voxel in rawVoxels)
                {
                    voxel.Add(VoxelsOffset.X, VoxelsOffset.Y, VoxelsOffset.Z);
                }
            } 
        }

        private void ConvertToMyFormat()
        {
            VoxelsInBlocks = new DictionaryVoxelsInBlock();
            if (rawVoxels.Count > 0) 
            {
                foreach (var voxel in rawVoxels)
                {
                    int blockPosX = (int)Math.Truncate(voxel.X / 16f);
                    int blockPosY = (int)Math.Truncate(voxel.Y / 16f);
                    int blockPosZ = (int)Math.Truncate(voxel.Z / 16f);

                    int voxPosX1 = (int)Math.Truncate((voxel.X - (blockPosX * 16)) / 8f);
                    int voxPosY1 = (int)Math.Truncate((voxel.Y - (blockPosY * 16)) / 8f);
                    int voxPosZ1 = (int)Math.Truncate((voxel.Z - (blockPosZ * 16)) / 8f);

                    int voxPosX2 = (int)Math.Truncate((voxel.X - (blockPosX * 16) - (voxPosX1 * 8)) / 4f);
                    int voxPosY2 = (int)Math.Truncate((voxel.Y - (blockPosY * 16) - (voxPosY1 * 8)) / 4f);
                    int voxPosZ2 = (int)Math.Truncate((voxel.Z - (blockPosZ * 16) - (voxPosZ1 * 8)) / 4f);

                    int voxPosX3 = (int)Math.Truncate((voxel.X - (blockPosX * 16) - (voxPosX1 * 8) - (voxPosX2 * 4)) / 2f);
                    int voxPosY3 = (int)Math.Truncate((voxel.Y - (blockPosY * 16) - (voxPosY1 * 8) - (voxPosY2 * 4)) / 2f);
                    int voxPosZ3 = (int)Math.Truncate((voxel.Z - (blockPosZ * 16) - (voxPosZ1 * 8) - (voxPosZ2 * 4)) / 2f);

                    int voxPosX4 = (int)Math.Truncate((voxel.X - (blockPosX * 16) - (voxPosX1 * 8) - (voxPosX2 * 4) - (voxPosX3 * 2)) / 1f);
                    int voxPosY4 = (int)Math.Truncate((voxel.Y - (blockPosY * 16) - (voxPosY1 * 8) - (voxPosY2 * 4) - (voxPosY3 * 2)) / 1f);
                    int voxPosZ4 = (int)Math.Truncate((voxel.Z - (blockPosZ * 16) - (voxPosZ1 * 8) - (voxPosZ2 * 4) - (voxPosZ3 * 2)) / 1f);

                    BlockPos blockPos = new BlockPos(blockPosX - BlockCorrection.X, blockPosY - BlockCorrection.Y, blockPosZ - BlockCorrection.Z, 0);
                    Vec3i voxPos1 = new Vec3i(voxPosX1, voxPosY1, voxPosZ1);
                    Vec3i voxPos2 = new Vec3i(voxPosX2, voxPosY2, voxPosZ2);
                    Vec3i voxPos3 = new Vec3i(voxPosX3, voxPosY3, voxPosZ3);
                    Vec3i voxPos4 = new Vec3i(voxPosX4, voxPosY4, voxPosZ4);

                    VoxelsInBlocks.TryAdd(blockPos, new Dictionary<Vec3i, Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>>());
                    VoxelsInBlocks[blockPos].TryAdd(voxPos1, new Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>());
                    VoxelsInBlocks[blockPos][voxPos1].TryAdd(voxPos2, new Dictionary<Vec3i, List<Vec3i>>());
                    VoxelsInBlocks[blockPos][voxPos1][voxPos2].TryAdd(voxPos3, new List<Vec3i>());
                    VoxelsInBlocks[blockPos][voxPos1][voxPos2][voxPos3].Add(voxPos4);
                }
            }
            
        }

        private void ApplyBlockOffset()
        {
            if(BlocksOffset != new Vec3i(0, 0, 0)) 
            {
                foreach (var block in VoxelsInBlocks.ToList())
                {
                    VoxelsInBlocks.Remove(block.Key);
                    VoxelsInBlocks.Add(block.Key.Add(BlocksOffset.X, BlocksOffset.Y, BlocksOffset.Z), block.Value);
                }
            }
        }

        public void SetBlockOffset(Vec3i offset) 
        {
            BlocksOffset = offset;
            UpdateModel();
        }

        public void SetVoxelsOffset(Vec3i offset)
        {
            VoxelsOffset = offset;
            UpdateModel();
        }

        public void SetAllignModel(string align) 
        {
            Allign = align;
            UpdateModel();
        }

        private void UpdateModel() 
        {
            ApplyAllignModel();
            ApplyVoxelsOffset();
            ConvertToMyFormat();
            ApplyBlockOffset();
            FindMinMaxPos();
        }

        public DictionaryVoxelsInBlock GetVoxelsInBlocks() 
        {
            DictionaryVoxelsInBlock cloneDictionary = new DictionaryVoxelsInBlock();
            foreach (var layer0 in VoxelsInBlocks)
            {
                foreach (var layer1 in layer0.Value)
                {
                    foreach (var layer2 in layer1.Value)
                    {
                        foreach (var layer3 in layer2.Value)
                        {
                            foreach (var layer4 in layer3.Value)
                            {
                                cloneDictionary.TryAdd(layer0.Key, new Dictionary<Vec3i, Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>>());
                                cloneDictionary[layer0.Key].TryAdd(layer1.Key, new Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>());
                                cloneDictionary[layer0.Key][layer1.Key].TryAdd(layer2.Key, new Dictionary<Vec3i, List<Vec3i>>());
                                cloneDictionary[layer0.Key][layer1.Key][layer2.Key].TryAdd(layer3.Key, new List<Vec3i>());
                                cloneDictionary[layer0.Key][layer1.Key][layer2.Key][layer3.Key].Add(layer4);

                            }
                        }
                    }
                }
            }
            return cloneDictionary;
        }

        private void FindMinMaxPos()
        {
            int minX = VoxelsInBlocks.First().Key.X;
            int minY = VoxelsInBlocks.First().Key.Y;
            int minZ = VoxelsInBlocks.First().Key.Z;
            int maxX = VoxelsInBlocks.First().Key.X;
            int maxY = VoxelsInBlocks.First().Key.Y;
            int maxZ = VoxelsInBlocks.First().Key.Z;
            foreach (var block in VoxelsInBlocks)
            {
                if (block.Key.X < minX)
                    minX = block.Key.X;
                if (block.Key.Y < minY)
                    minY = block.Key.Y;
                if (block.Key.Z < minZ)
                    minZ = block.Key.Z;
                if (block.Key.X > maxX)
                    maxX = block.Key.X;
                if (block.Key.Y > maxY)
                    maxY = block.Key.Y;
                if (block.Key.Z > maxZ)
                    maxZ = block.Key.Z;
            }
            MinPos = new Vec3i(minX, minY, minZ);
            MaxPos = new Vec3i(maxX, maxY, maxZ);
        }

        public List<BlockPos> GetRequiredBlocksList()
        {
            List<BlockPos> requiredBlocks = new List<BlockPos>();
            foreach (var block in VoxelsInBlocks)
            {
                requiredBlocks.Add(block.Key);
            }
            return requiredBlocks;
        }

        public List<BlockPos> GetDimensionsBlocksList()
        {
            List<BlockPos> dimensionsBlocks = new List<BlockPos>();

            for (int X = MinPos.X; X <= MaxPos.X; X++)
            {
                for (int Y = MinPos.Y; Y <= MaxPos.Y; Y++)
                {
                    for (int Z = MinPos.Z; Z <= MaxPos.Z; Z++)
                    {
                        dimensionsBlocks.Add(new BlockPos(X, Y, Z, 0));
                    }
                }
            }
            return dimensionsBlocks;

        }

        public List<BlockPos> GetModelHighlightList()
        {
            List<BlockPos> highlightList = new List<BlockPos> ();
            foreach (var block in VoxelsInBlocks)
            {
                highlightList.Add(block.Key);
            }
            return highlightList;
        }

        public List<BlockPos> GetDimensionsHighlightList() 
        {
            List<BlockPos> highlightList = new List<BlockPos>();
            highlightList.Add(new BlockPos(MinPos, 0));
            highlightList.Add(new BlockPos(MaxPos.Clone().Add(1,1,1), 0));
            return highlightList;
        }

        public MyVoxFormat Clone() 
        {
            MyVoxFormat clone = new MyVoxFormat(voxFileName);
            clone.SetAllignModel(Allign);
            clone.SetVoxelsOffset(VoxelsOffset);
            clone.SetBlockOffset(BlocksOffset);
            return clone;
        }

    }
}
