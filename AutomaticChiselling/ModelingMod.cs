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
using Vintagestory.API.Common.CommandAbbr;




namespace AutomaticChiselling
{
    public enum ModelAllign
    {
        Center,
        Northeast,
        Northwest,
        Southeast,
        Southwest
    }

    public class AutomaticChiselling : ModSystem
    {
        

        private ICoreClientAPI capi;
        private VoxelsStorage storage;
        private ChiselConveyor conveyor;
        private ModelAllign allign = ModelAllign.Center;
        private Vec3i blocksOffset = new Vec3i(0, 0, 0);
        private Vec3i voxelsOffset = new Vec3i(0, 0, 0);

        static int hLMDID = 500;
        static int hLMBID = 501;


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            RegisterVoxCommands(api);
            Patchs.PatchAll();

            // api.Logger.Notification("Hello from template mod client side: " + Lang.Get("anvys:hello"));
        }

        private void RegisterVoxCommands(ICoreAPI api) 
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            string[] _alligns = { "center", "northeast", "northwest", "southeast", "southwest" };
            api.ChatCommands.Create("vox")
            .RequiresPlayer()


            .BeginSubCommand("load")
            .WithDescription("Loads the .vox model into the buffer from the world edit folder")
            .WithArgs(new ICommandArgumentParser[]
            {
                parsers.Word("file name"),
            })
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return LoadVoxelsFromFile(args);
            })
            .EndSub()


            .BeginSubCommand("selpos")
            .WithDescription("Marks the starting position and displays a preview.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return SelectStartPos(args);
            })
            .EndSub()


            .BeginSubCommand("start")
            .WithDescription("Starts the chisel process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return StartChiseling(args);
            })
            .EndSub()


            .BeginSubCommand("hidehl")
            .WithDescription("Disables block highlighting.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return DisablesHL(args);
            })
            .EndSub()


            .BeginSubCommand("pause")
            .WithDescription("Pauses the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return PauseChiseling(args);
            })
            .EndSub()


            .BeginSubCommand("resume")
            .WithDescription("Resume the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return ResumeChiseling(args);
            })
            .EndSub()


            .BeginSubCommand("stop")
            .WithDescription("Stop the chiselling process.")
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return StopChiseling(args);
            })
            .EndSub()


            .BeginSubCommand("setplimit")
            .WithDescription("Set packet limit per iteration.")
            .WithArgs(new ICommandArgumentParser[]
            {
                parsers.IntRange("Packet limit per iteration", 1, 100)
            })
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return SetPacketLimit(args);
            })
            .EndSub()


            .BeginSubCommand("setallign")
            .WithDescription("Sets at what corner of the dimensional cube the model should be aligned.")
            .WithArgs(new ICommandArgumentParser[]
            {
                parsers.WordRange("allign", _alligns)
            })
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return SetAllign(args);
            })
            .EndSub()

            .BeginSubCommand("setvoxoffset")
            .WithDescription("Sets voxels offset.")
            .WithArgs(new ICommandArgumentParser[]
            {
                parsers.Vec3i("voxels offset")
            })
            .HandleWith(delegate (TextCommandCallingArgs args)
            {
                return SetVoxelsOffset(args);
            })
            .EndSub();
        }


        public TextCommandResult LoadVoxelsFromFile(TextCommandCallingArgs args)
        {
            if (conveyor != null)
            {
                if (conveyor.СhisellingActive())
                {
                    return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .vox stop");
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

            storage = new VoxelsStorage(filename);

            voxelsOffset = new Vec3i(0, 0, 0);

            return TextCommandResult.Success("The model was successfully loaded.");
        }

        public TextCommandResult SelectStartPos(TextCommandCallingArgs args)
        {
            if (storage == null) 
            {
                return TextCommandResult.Success("Model not loaded. Use .vox load [filename] first.");
            }

            if (conveyor != null)
            {
                if (conveyor.СhisellingActive())
                {
                    return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .vox stop");
                }
            }

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

            blocksOffset = startPos;

            storage.SetBlockVoxelsOffsetAndAllign(blocksOffset, voxelsOffset, allign);

            HiLightModelBlocks(storage.GetModelHighlightList());
            HiLightmodelDimension(storage.GetDimensionsHighlightList());

            return TextCommandResult.Success("The starting position has been successfully selected.");
        }

        public TextCommandResult StartChiseling(TextCommandCallingArgs args)
        {
            if (storage == null)
            {
                return TextCommandResult.Success("Model not loaded. Use .vox load [filename] first.");
            }
            if (conveyor != null)
            {
                if (conveyor.СhisellingActive())
                {
                    return TextCommandResult.Success("You are in active chiselling mode, or wait for the process to complete or stop it with the command .voxstop");
                }
            }

            conveyor = new ChiselConveyor(capi, storage);
            conveyor.StartConveyor();
            return TextCommandResult.Success("Chiseled start.");
        }

        public TextCommandResult DisablesHL(TextCommandCallingArgs args) 
        {
            ClearHiLightmodelDimension();
            ClearHiLightModelBlocks();
            return TextCommandResult.Success("Disabled.");
        }

        private TextCommandResult PauseChiseling(TextCommandCallingArgs args) 
        {
            conveyor.PauseConveyor();
            return TextCommandResult.Success("Paused.");
        }

        private TextCommandResult ResumeChiseling(TextCommandCallingArgs args)
        {
            conveyor.ResumeConveyor();
            return TextCommandResult.Success("Resumed.");
        }

        private TextCommandResult StopChiseling(TextCommandCallingArgs args)
        {
            conveyor.StopConveyor();
            ClearHiLightmodelDimension();
            ClearHiLightModelBlocks();
            return TextCommandResult.Success("Stoped.");
        }

        private TextCommandResult SetPacketLimit(TextCommandCallingArgs args)
        {
            if (!capi.IsSinglePlayer)
            {
                return TextCommandResult.Success("Available for single player only.");
            }

            if (conveyor == null)
            {
                return TextCommandResult.Success("First start the chiseling process");
            }

            if (!conveyor.СhisellingActive())
            {
                return TextCommandResult.Success("First start the chiseling process");
            }

            if (!conveyor.SetPacketPerIteration((int)args[0]))
            {
                return TextCommandResult.Success("Packet limit not set.");
            }

            return TextCommandResult.Success("Packet limit set.");

        }

        private TextCommandResult SetAllign(TextCommandCallingArgs args)
        {
            if (storage == null)
            {
                return TextCommandResult.Success("Model not loaded. Use .vox load [filename] first.");
            }

            string _allign = (string)args[0];

            if (string.IsNullOrEmpty(_allign)) 
            {
                return TextCommandResult.Success("Invalid parameter.");
            }
            switch (_allign)
            {
                case "center":
                    allign = ModelAllign.Center;
                    break;

                case "northeast":
                    allign = ModelAllign.Northeast;
                    break;

                case "northwest":
                    allign = ModelAllign.Northwest;
                    break;

                case "southeast":
                    allign = ModelAllign.Southeast;
                    break;

                case "southwest":
                    allign = ModelAllign.Southwest;
                    break;

                default:
                    return TextCommandResult.Success("Invalid parameter.");
            }

            storage.SetBlockVoxelsOffsetAndAllign(blocksOffset, voxelsOffset, allign);
            HiLightModelBlocks(storage.GetModelHighlightList());
            HiLightmodelDimension(storage.GetDimensionsHighlightList());
            return TextCommandResult.Success("Alignment successfully applied.");
            
        }

        private TextCommandResult SetVoxelsOffset(TextCommandCallingArgs args)
        {
            if (storage == null)
            {
                return TextCommandResult.Success("Model not loaded. Use .vox load [filename] first.");
            }
            Vec3i offset = (Vec3i)args[0];

            if (offset == null) 
            {
                return TextCommandResult.Success("Invalid parameter.");
            }
            voxelsOffset = offset;

            storage.SetBlockVoxelsOffsetAndAllign(blocksOffset, voxelsOffset, allign);
            HiLightModelBlocks(storage.GetModelHighlightList());
            HiLightmodelDimension(storage.GetDimensionsHighlightList());
            return TextCommandResult.Success("Voxel offset successfully applied.");

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


    }
}
