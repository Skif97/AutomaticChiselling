using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Client;
using Vintagestory.GameContent;
using VoxReader;
using VoxReader.Interfaces;
using Vintagestory.Common;
using System.Collections.ObjectModel;


namespace AutomaticChiselling
{

    public class AutomaticChiselling : ModSystem
    {
        

        private ICoreClientAPI capi;
        private MyVoxFormat myVox;
        private ChiselConveyor chiselConveyor;

        static int hLMDID = 500;
        static int hLMBID = 501;


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            RegisterLoadModel(api);
            RegisterStartChiseling(api);
            RegisterSelectStartPos(api);
            RegisterDisablesHL(api);
            RegisterPauseChiseling(api);
            RegisterResumeChiseling(api);
            RegisterStopChiseling(api);
            //RegisterTestChiseling(api);

            // api.Logger.Notification("Hello from template mod client side: " + Lang.Get("anvys:hello"));
        }



        private void RegisterLoadModel(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxload")
            .RequiresPlayer()
            .WithDescription("Loads the .vox model into the buffer from the world edit folder")
            .WithArgs(new ICommandArgumentParser[]
            {
                parsers.Word("file name"),
            })
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return LoadVoxelsFromFile(args);
            });
        }

        public TextCommandResult LoadVoxelsFromFile(TextCommandCallingArgs args)
        {
            if (chiselConveyor != null)
            {
                if (chiselConveyor.СhisellingActive())
                {
                    return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .voxstop");
                }
            }


            string filename = (string)args[0];

            if (filename == "")
            {
                return TextCommandResult.Success("Model not selected.");
            }

            string basePath = Path.Combine(GamePaths.DataPath, "worldedit");
            basePath = Path.Combine(basePath, filename) + ".vox";
            if (!File.Exists(basePath))
            {
                return TextCommandResult.Success("Model not found.");
            }

            IVoxFile voxFile;
            try
            {
                voxFile = VoxReader.VoxReader.Read(basePath);
            }
            catch (FileNotFoundException e)
            {
                return TextCommandResult.Success("Errors occurred while reading the file.");
            }

            myVox = new MyVoxFormat(filename);

            return TextCommandResult.Success("The model was successfully loaded.");
        }

        private void RegisterSelectStartPos(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxselpos")
            .RequiresPlayer()
            .WithDescription("Marks the starting position and displays a preview.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return SelectStartPos(args);
            });
        }


        public TextCommandResult SelectStartPos(TextCommandCallingArgs args)
        {
            BlockSelection localBlockPos = capi.World.Player.Entity.BlockSelection;

            if (localBlockPos == null)
            {
                return TextCommandResult.Success("No starting position has been selected. Look at the desired block.");
            }
            Vec3i startPos = localBlockPos.Position.ToVec3i();
            if (startPos == null)
            {
                return TextCommandResult.Success("The starting position has not been selected. Look at the desired block.");
            }

            if (chiselConveyor!=null )
            {
                if (chiselConveyor.СhisellingActive())
                {
                    return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .voxstop");
                }
            }

            myVox.SetBlockOffset(startPos);

            HiLightModelBlocks(myVox.GetModelHighlightList());
            HiLightmodelDimension(myVox.GetDimensionsHighlightList());

            return TextCommandResult.Success("The starting position has been successfully selected.");
        }



        private void RegisterStartChiseling(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxstart")
            .RequiresPlayer()
            .WithDescription("Starts the chisel process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                if (chiselConveyor != null)
                {
                    if (chiselConveyor.СhisellingActive())
                    {
                        return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .voxstop");
                    }
                }

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
                    return TextCommandResult.Success("The chisel is missing in your hands!");
                }
                if (!capi.IsSinglePlayer) 
                {
                    return TextCommandResult.Success("Currently this mod is for single player only. *artificial limitation, will be disabled in future releases*");
                }


                return StartChiseling(args);
            });
        }


       

        public TextCommandResult StartChiseling(TextCommandCallingArgs args)
        {
            chiselConveyor = new ChiselConveyor(capi, myVox);
            chiselConveyor.StartConveyor();
            return TextCommandResult.Success("Chiseled start.");
        }

        private void RegisterDisablesHL(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxhidehl")
            .RequiresPlayer()
            .WithDescription("Disables block highlighting.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return DisablesHL(args);
            });
        }

        public TextCommandResult DisablesHL(TextCommandCallingArgs args) 
        {
            ClearHiLightmodelDimension();
            ClearHiLightModelBlocks();
            return TextCommandResult.Success("Disabled.");
        }

        private void RegisterPauseChiseling(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxpause")
            .RequiresPlayer()
            .WithDescription("Pauses the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return PauseChiseling(args);
            });
        }

        private TextCommandResult PauseChiseling(TextCommandCallingArgs args) 
        {
            chiselConveyor.PauseConveyor();
            return TextCommandResult.Success("Paused.");
        }



        private void RegisterResumeChiseling(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxresume")
            .RequiresPlayer()
            .WithDescription("Resume the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return ResumeChiseling(args);
            });
        }

        private TextCommandResult ResumeChiseling(TextCommandCallingArgs args)
        {
            chiselConveyor.ResumeConveyor();
            return TextCommandResult.Success("Resumed.");
        }

        private void RegisterStopChiseling(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("voxstop")
            .RequiresPlayer()
            .WithDescription("Stop the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return StopChiseling(args);
            });
        }

        private TextCommandResult StopChiseling(TextCommandCallingArgs args)
        {
            chiselConveyor.StopConveyor();
            ClearHiLightmodelDimension();
            ClearHiLightModelBlocks();
            return TextCommandResult.Success("Stoped.");
        }



        private void HiLightModelBlocks(List<BlockPos> locakBlockPosList) 
        {       
            List<int> colors = new List<int> { ColorUtil.ToRgba(100, (int)(0 % 256), (int)(250 % 256), (int)(0 % 256)) };
            capi.World.HighlightBlocks(capi.World.Player, hLMBID, locakBlockPosList, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
        }

        private void HiLightmodelDimension(List<BlockPos> locakBlockPosList) 
        {
            List<int> colors = new List<int> { ColorUtil.ToRgba(100, (int)(0 % 256), (int)(0 % 256), (int)(250 % 256)) };
            capi.World.HighlightBlocks(capi.World.Player, hLMDID, locakBlockPosList, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
        }

        private void ClearHiLightmodelDimension()
        {
            capi.World.HighlightBlocks(capi.World.Player, hLMDID, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
        }

        private void ClearHiLightModelBlocks()
        {
            capi.World.HighlightBlocks(capi.World.Player, hLMBID, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
        }


        //static void RegisterTestChiseling(ICoreAPI api)
        //{
        //    CommandArgumentParsers parsers = api.ChatCommands.Parsers;
        //    api.ChatCommands.Create("voxelconvertertestchiseling")
        //    .WithAlias("vctc")
        //    .RequiresPlayer()
        //    .WithDescription("Тестирует чизл")
        //    .HandleWith(delegate (TextCommandCallingArgs args)
        //    {
        //        return StartTestChiseling(args);
        //    });
        //}

        //public TextCommandResult StartTestChiseling(TextCommandCallingArgs args)
        //{
        //    IPlayerInventoryManager inventoryManager = capi.World.Player.InventoryManager;
        //    ItemSlot itemSlot = ((inventoryManager != null) ? inventoryManager.ActiveHotbarSlot : null);
        //    ItemSlot slot = itemSlot;
        //    object obj;
        //    if (slot == null)
        //    {
        //        obj = null;
        //    }
        //    else
        //    {
        //        ItemStack itemstack = slot.Itemstack;
        //        obj = ((itemstack != null) ? itemstack.Collectible : null);
        //    }
        //    ItemChisel itemChisel = obj as ItemChisel;
        //    if (itemChisel == null)
        //    {
        //        return TextCommandResult.Success("В руках нету  зубила");
        //    }
        //    if (itemChisel.GetToolMode(slot, capi.World.Player, capi.World.Player.CurrentBlockSelection) != 3)
        //    {
        //        SetToolMode(3, capi.World.Player.Entity.BlockSelection);
        //    }
        //    var blockType = capi.World.BlockAccessor.GetBlockEntity(capi.World.Player.Entity.BlockSelection.Position);
        //    if (blockType == null || !(blockType is BlockEntityChisel))
        //    {
        //        BlockSelection bs = new BlockSelection();

        //        (capi.World as ClientMain)?.SendHandInteraction(2, capi.World.Player.Entity.BlockSelection, null, EnumHandInteract.HeldItemInteract, EnumHandInteractNw.StartHeldItemUse, true);
        //        //return TextCommandResult.Success("Вы выбрали не чизловый блок");
        //    }

        //    // Vec3i voxelPos = new Vec3i(x1 * 8, y1 * 8, z1 * 8);
        //    Vec3i bpos = capi.World.Player.Entity.BlockSelection.Position.ToVec3i();
        //    Vec3i voxelPos = new Vec3i(4, 4, 4);

        //    byte[] data;

        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        BinaryWriter writer = new BinaryWriter(ms);
        //        writer.Write(voxelPos.X);
        //        writer.Write(voxelPos.Y);
        //        writer.Write(voxelPos.Z);
        //        //isBreak, если false - то поставить пиксель
        //        writer.Write(true);
        //        writer.Write(BlockFacing.indexWEST);
        //        //newmaterial , id материала по отношению от индекса в текущем блоке, игнорируется при isBreak true
        //        writer.Write((byte)Math.Max(0, 1));
        //        data = ms.ToArray();
        //    }
        //    (capi.World as ClientMain)?.SendBlockEntityPacket(bpos.X, bpos.Y, bpos.Z, (int)1010, data);





        //    return TextCommandResult.Success("Test start.");
        //}









    }
}
