using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using VSSurvivalMod.Systems.ChiselModes;
using AutomaticChiselling;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.Client;

namespace AutomaticChiselling
{
    using DictionaryVoxelsInBlock = Dictionary<BlockPos, Dictionary<Vec3i, Dictionary<Vec3i, Dictionary<Vec3i, List<Vec3i>>>>>;
    public static class VoxelCore
    {

        private static List<Vec3i> voxelCore = new List<Vec3i>{
            new Vec3i(0,0,0),
            new Vec3i(0,0,1),
            new Vec3i(0,1,0),
            new Vec3i(0,1,1),
            new Vec3i(1,0,0),
            new Vec3i(1,0,1),
            new Vec3i(1,1,0),
            new Vec3i(1,1,1)
        };
        public static ReadOnlyCollection<Vec3i> Voxels => voxelCore.AsReadOnly();
    }


    public class ChiselConveyor
    {
        ICoreClientAPI capi;

        // private int blockPos = 0;
        private int iteratorLayer1 = 0;
        private int iteratorLayer2 = 0;
        private int iteratorLayer3 = 0;
        private int iteratorLayer4 = 0;
        //  private BlockPos lastBlockPos;
        // private int[] baseTimeforTiker = { 25, 20, 15, 10, 5};
        private int stage = 0;
        // private int totalProgres = 0;
        private List<BlockPos> requiredBlocksList;
        private List<BlockPos> dimensionsBlocksList;
        private DictionaryVoxelsInBlock voxelsInBlock;
        private long tickerID;
        private MyVoxFormat myVox;
        private bool acticeChiseling = false; 
        long lastReminder = 0;

        public ChiselConveyor(ICoreClientAPI _capi, MyVoxFormat _myVox)
        {
            capi = _capi;
            myVox = _myVox;
            requiredBlocksList = myVox.GetRequiredBlocksList();
            dimensionsBlocksList = myVox.GetDimensionsBlocksList();
            voxelsInBlock = myVox.GetVoxelsInBlocks();
        }

        public void StartConveyor()
        {
            acticeChiseling = true;
            // blockPos = 0;
            stage = 0;
            iteratorLayer1 = 0;
            iteratorLayer2 = 0;
            iteratorLayer3 = 0;
            iteratorLayer4 = 0;
            tickerID = capi.World.RegisterGameTickListener(OneStep, 25);
        }

        public void PauseConveyor()
        {
            capi.World.UnregisterGameTickListener(tickerID);
        }

        public void ResumeConveyor()
        {
            tickerID = capi.World.RegisterGameTickListener(OneStep, 25);
        }

        public void StopConveyor()
        {
            acticeChiseling = false ;
            capi.World.UnregisterGameTickListener(tickerID);
            //blockPos = 0;
            stage = 0;
            iteratorLayer1 = 0;
            iteratorLayer2 = 0;
            iteratorLayer3 = 0;
            iteratorLayer4 = 0;
        }

        public bool СhisellingActive() 
        {
            return acticeChiseling;
        }

        private bool Stage0()
        {
            bool flag = false;
            BlockPos cureentPos = dimensionsBlocksList.First();
            //lastBlockPos = cureentPos
            if (!myVox.GetRequiredBlocksList().Contains(cureentPos))
            {
                if (!ChiselDetecter())
                {
                    ChiselReminder();
                    return true;
                }
                flag = BreakBlock(cureentPos);
            }

            dimensionsBlocksList.Remove(cureentPos);
            if (dimensionsBlocksList.Count == 0)
            {
                stage++;
                return true;
            }
            return flag;
        }

        private bool Stage1()
        {
            bool flag = false;
            BlockPos cureentPos = requiredBlocksList.First();
            //lastBlockPos = cureentPos;
            if (!voxelsInBlock[cureentPos].ContainsKey(VoxelCore.Voxels[iteratorLayer1]))
            {
                if (!ChiselDetecter())
                {
                    ChiselReminder();
                    return true;
                }
                OneStepChiseling(cureentPos, 3, VoxelCore.Voxels[iteratorLayer1], VoxelCore.Voxels[0], VoxelCore.Voxels[0], VoxelCore.Voxels[0]);
                flag = true;
            }
            iteratorLayer1++;
            if (iteratorLayer1 > 7)
            {
                iteratorLayer1 = 0;
                requiredBlocksList.Remove(cureentPos);
            }
            if (requiredBlocksList.Count == 0)
            {
                requiredBlocksList = myVox.GetRequiredBlocksList();
                stage++;
                return true;
            }
            return flag;
        }

        private bool Stage2()
        {
            bool flag = false;
            BlockPos cureentPos = requiredBlocksList.First();
            //lastBlockPos = cureentPos;
            if (voxelsInBlock[cureentPos].ContainsKey(VoxelCore.Voxels[iteratorLayer1]))
            {
                if (!voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]].ContainsKey(VoxelCore.Voxels[iteratorLayer2]))
                {
                    if (!ChiselDetecter())
                    {
                        ChiselReminder();
                        return true;
                    }
                    OneStepChiseling(cureentPos, 2, VoxelCore.Voxels[iteratorLayer1], VoxelCore.Voxels[iteratorLayer2], VoxelCore.Voxels[0], VoxelCore.Voxels[0]);
                    flag = true;
                }
            }
            iteratorLayer2++;
            if (iteratorLayer2 > 7)
            {
                iteratorLayer2 = 0;
                iteratorLayer1++;
            }
            if (iteratorLayer1 > 7)
            {
                iteratorLayer1 = 0;
                requiredBlocksList.Remove(cureentPos);
            }
            if (requiredBlocksList.Count == 0)
            {
                requiredBlocksList = myVox.GetRequiredBlocksList();
                stage++;
                return true;
            }
            return flag;
        }

        private bool Stage3()
        {
            bool flag = false;
            BlockPos cureentPos = requiredBlocksList.First();
            //lastBlockPos = cureentPos;
            if (voxelsInBlock[cureentPos].ContainsKey(VoxelCore.Voxels[iteratorLayer1]))
            {
                if (voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]].ContainsKey(VoxelCore.Voxels[iteratorLayer2]))
                {
                    if (!voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]][VoxelCore.Voxels[iteratorLayer2]].ContainsKey(VoxelCore.Voxels[iteratorLayer3]))
                    {
                        if (!ChiselDetecter())
                        {
                            ChiselReminder();
                            return true;
                        }
                        OneStepChiseling(cureentPos, 1, VoxelCore.Voxels[iteratorLayer1], VoxelCore.Voxels[iteratorLayer2], VoxelCore.Voxels[iteratorLayer3], VoxelCore.Voxels[0]);
                        flag = true;
                    }
                }
            }
            iteratorLayer3++;
            if (iteratorLayer3 > 7)
            {
                iteratorLayer3 = 0;
                iteratorLayer2++;
            }
            if (iteratorLayer2 > 7)
            {
                iteratorLayer2 = 0;
                iteratorLayer1++;
            }
            if (iteratorLayer1 > 7)
            {
                iteratorLayer1 = 0;
                requiredBlocksList.Remove(cureentPos);
            }
            if (requiredBlocksList.Count == 0)
            {
                requiredBlocksList = myVox.GetRequiredBlocksList();
                stage++;
                return true;
            }
            return flag;
        }

        private bool Stage4()
        {
            bool flag = false;
            BlockPos cureentPos = requiredBlocksList.First();
            //lastBlockPos = cureentPos;
            if (voxelsInBlock[cureentPos].ContainsKey(VoxelCore.Voxels[iteratorLayer1]))
            {
                if (voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]].ContainsKey(VoxelCore.Voxels[iteratorLayer2]))
                {
                    if (voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]][VoxelCore.Voxels[iteratorLayer2]].ContainsKey(VoxelCore.Voxels[iteratorLayer3]))
                    {
                        if (!voxelsInBlock[cureentPos][VoxelCore.Voxels[iteratorLayer1]][VoxelCore.Voxels[iteratorLayer2]][VoxelCore.Voxels[iteratorLayer3]].Contains(VoxelCore.Voxels[iteratorLayer4]))
                        {
                            if (!ChiselDetecter()) 
                            {
                                ChiselReminder();
                                return true;
                            }
                            OneStepChiseling(cureentPos, 0, VoxelCore.Voxels[iteratorLayer1], VoxelCore.Voxels[iteratorLayer2], VoxelCore.Voxels[iteratorLayer3], VoxelCore.Voxels[iteratorLayer4]);
                            flag = true;
                        }
                    }
                }
            }
            iteratorLayer4++;
            if (iteratorLayer4 > 7)
            {
                iteratorLayer4 = 0;
                iteratorLayer3++;
            }
            if (iteratorLayer3 > 7)
            {
                iteratorLayer3 = 0;
                iteratorLayer2++;
            }
            if (iteratorLayer2 > 7)
            {
                iteratorLayer2 = 0;
                iteratorLayer1++;
            }
            if (iteratorLayer1 > 7)
            {
                iteratorLayer1 = 0;
                requiredBlocksList.Remove(cureentPos);
            }
            if (requiredBlocksList.Count == 0)
            {
                requiredBlocksList = myVox.GetRequiredBlocksList();
                stage++;
                return true;
            }
            return flag;
        }


        private void ChiselReminder()
        {
            if(capi.ElapsedMilliseconds - lastReminder > 2000) 
            {
                capi.ShowChatMessage("Take the chisel in your hand and don't let go!!!");
                lastReminder = capi.ElapsedMilliseconds;
            }
        }

        private bool ChiselDetecter() 
        {
            IPlayerInventoryManager inventoryManager = capi.World.Player.InventoryManager;
            ItemSlot itemSlot = ((inventoryManager != null) ? inventoryManager.ActiveHotbarSlot : null);
            ItemSlot slot = itemSlot;
            object obj;
            if (slot == null)
            {
                obj = null;
            }
            else
            {
                ItemStack itemstack = slot.Itemstack;
                obj = ((itemstack != null) ? itemstack.Collectible : null);
            }
            ItemChisel itemChisel = obj as ItemChisel;
            if (itemChisel == null)
            {
                return false;
            }
            return true;
        }


        private void OneStep(float time) 
        {
            if (stage == 0)
            {
                while (!Stage0());
            }
            if (stage == 1) 
            {
                while (!Stage1()) ;
            }
            if (stage == 2) 
            {
                while (!Stage2()) ;
            }
            if (stage == 3) 
            {
                while (!Stage3()) ;
            }
            if (stage == 4) 
            {
                while (!Stage4()) ;
            }

            if (stage >= 5) 
            { 
                StopConveyor();
                capi.ShowChatMessage("Chiseling completed!!!");
            }

        }


        private bool OneStepChiseling(BlockPos localBlockPos, int toolMode, Vec3i layer1, Vec3i layer2, Vec3i layer3, Vec3i layer4)
        {

            Vec3i voxPosition = new Vec3i(
                                    (layer1.X * 8) +
                                    (layer2.X * 4) +
                                    (layer3.X * 2) +
                                    (layer4.X * 1),

                                    (layer1.Y * 8) +
                                    (layer2.Y * 4) +
                                    (layer3.Y * 2) +
                                    (layer4.Y * 1),

                                    (layer1.Z * 8) +
                                    (layer2.Z * 4) +
                                    (layer3.Z * 2) +
                                    (layer4.Z * 1)
                                    );
            BlockSelection blockSel = new BlockSelection(localBlockPos, BlockFacing.NORTH, capi.World.BlockAccessor.GetBlock(localBlockPos));

            IPlayerInventoryManager inventoryManager = capi.World.Player.InventoryManager;
            ItemSlot itemSlot = ((inventoryManager != null) ? inventoryManager.ActiveHotbarSlot : null);
            ItemSlot slot = itemSlot;
            object obj;
            if (slot == null)
            {
                obj = null;
            }
            else
            {
                ItemStack itemstack = slot.Itemstack;
                obj = ((itemstack != null) ? itemstack.Collectible : null);
            }
            ItemChisel itemChisel = obj as ItemChisel;
            if (itemChisel == null)
            {
                //capi.ShowChatMessage("В руках нету  зубила");
                return false;
            }

            if (itemChisel.GetToolMode(slot, capi.World.Player, blockSel) != toolMode)
            {
                SetToolMode(toolMode, blockSel);
                // capi.ShowChatMessage("Размер откалывания, несоответствует, установлен на: " + size);
            }

            if (localBlockPos == null )
            {
                // capi.ShowChatMessage("Позиция блока не установлена.");
                return false;
            }

            var blockType = capi.World.BlockAccessor.GetBlock(localBlockPos);
            if (blockType == null || blockType.Id == 0)
            {
                // capi.ShowChatMessage("В заданной позиции блок не найден.");
                return false;
            }

            var blockEntity = capi.World.BlockAccessor.GetBlockEntity(localBlockPos);
            if (blockEntity == null || !(blockEntity is BlockEntityChisel))
            {

                (capi.World as ClientMain)?.SendHandInteraction(2, blockSel, null, EnumHandInteract.HeldItemInteract, EnumHandInteractNw.StartHeldItemUse, true);
                //capi.ShowChatMessage("В заданной позиции был цельный блок, сконвертировано в чизленый.");
            }

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(voxPosition.X);
                writer.Write(voxPosition.Y);
                writer.Write(voxPosition.Z);
                //isBreak, если false - то поставить пиксель
                writer.Write(true);
                writer.Write(BlockFacing.indexWEST);
                //newmaterial , id материала по отношению от индекса в текущем блоке, игнорируется при isBreak true
                writer.Write((byte)Math.Max(0, 1));
                data = ms.ToArray();
            }
            (capi.World as ClientMain)?.SendBlockEntityPacket(localBlockPos.X, localBlockPos.Y, localBlockPos.Z, (int)1010, data);
            // capi.ShowChatMessage("Блок успешно отколот.");
            return true;
        }

        private bool BreakBlock(BlockPos localBlockPos) 
        {

            if (capi.World.BlockAccessor.GetBlock(localBlockPos).BlockId != 0)
            {
                //EnumHandHandling eh = EnumHandHandling.NotHandled;
                var bs = new BlockSelection();
                bs.Position = localBlockPos;
                bs.Face = BlockFacing.NORTH;
                bs.HitPosition = localBlockPos.ToVec3d();
                bs.DidOffset = false;
                bs.SelectionBoxIndex = 0;

                (capi.World as ClientMain)?.SendPacketClient(ClientPackets.BlockInteraction(bs, 0, 0));
                // capi.World.BlockAccessor.BreakBlock(key.ToBlockPos(), capi.World.Player);
                // capi.World.BlockAccessor.GetBlock(key.ToBlockPos()).OnHeldAttackStart(capi.World.Player.InventoryManager.ActiveHotbarSlot, capi.World.Player.Entity, new BlockSelection(key.ToBlockPos(), BlockFacing.NORTH, null) , null, ref eh);
                return true;
            }
            return false;
        }


        private void SetToolMode(int num, BlockSelection blockSele)
        {
            ItemSlot slot = this.capi.World.Player.InventoryManager.ActiveHotbarSlot;
            CollectibleObject collectibleObject;
            if (slot == null)
            {
                collectibleObject = null;
            }
            else
            {
                ItemStack itemstack = slot.Itemstack;
                collectibleObject = ((itemstack != null) ? itemstack.Collectible : null);
            }
            CollectibleObject obj = collectibleObject;
            if (obj != null)
            {
                obj.SetToolMode(slot, this.capi.World.Player, blockSele, num);
                IClientNetworkAPI network = this.capi.Network;
                Packet_Client packet_Client = new Packet_Client();
                packet_Client.Id = 27;
                Packet_ToolMode packet_ToolMode = new Packet_ToolMode();
                packet_ToolMode.Mode = num;
                BlockSelection blockSelection = blockSele;
                packet_ToolMode.X = ((blockSelection != null) ? blockSelection.Position.X : 0);
                BlockSelection blockSelection2 = blockSele;
                packet_ToolMode.Y = ((blockSelection2 != null) ? blockSelection2.Position.Y : 0);
                BlockSelection blockSelection3 = blockSele;
                packet_ToolMode.Z = ((blockSelection3 != null) ? blockSelection3.Position.Z : 0);
                packet_Client.ToolMode = packet_ToolMode;
                network.SendPacketClient(packet_Client);
                slot.MarkDirty();
            }
        }



    }
}
