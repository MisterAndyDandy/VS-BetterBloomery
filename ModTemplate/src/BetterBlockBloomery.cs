﻿using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BetterBloomeries
{
    public class BetterBlockBloomery : Block, IIgnitable
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side != EnumAppSide.Client)
            {
                return;
            }

            ICoreClientAPI capi = api as ICoreClientAPI;
            interactions = ObjectCacheUtil.GetOrCreate(api, "betterBloomeryBlockInteractions", delegate
            {
                List<ItemStack> list = new List<ItemStack>();
                List<ItemStack> list2 = new List<ItemStack>();
                List<ItemStack> list3 = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    if (collectible.CombustibleProps != null)
                    {
                        if (collectible.CombustibleProps.SmeltedStack != null && collectible.CombustibleProps.MeltingPoint < 1500)
                        {
                            List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                            if (handBookStacks != null)
                            {
                                list.AddRange(handBookStacks);
                            }
                        }
                        else if (collectible.CombustibleProps.BurnTemperature >= 1200 && collectible.CombustibleProps.BurnDuration > 30f)
                        {
                            List<ItemStack> handBookStacks2 = collectible.GetHandBookStacks(capi);
                            if (handBookStacks2 != null)
                            {
                                list2.AddRange(handBookStacks2);
                            }
                        }
                    }
                }

                return new WorldInteraction[4]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-bloomery-heatable",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-bloomery-heatablex4",
                        HotKeyCode = "ctrl",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-bloomery-fuel",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list2.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-bloomery-ignite",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list3.ToArray(),
                        GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
                        {
                            BlockEntityBloomery blockEntityBloomery = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityBloomery;
                            return (blockEntityBloomery != null && blockEntityBloomery.CanIgnite() && !blockEntityBloomery.IsBurning && api.World.BlockAccessor.GetBlock(bs.Position.UpCopy()).Code.Path.Contains("betterbloomerychimney")) ? wi.Itemstacks : null;
                        }
                    }
                };
            });
        }

        private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
        {
            BlockEntityBloomery blockEntityBloomery = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityBloomery;
            if (blockEntityBloomery == null || wi.Itemstacks.Length == 0)
            {
                return null;
            }

            List<ItemStack> list = new List<ItemStack>();
            ItemStack[] itemstacks = wi.Itemstacks;
            foreach (ItemStack itemStack in itemstacks)
            {
                if (blockEntityBloomery.CanAdd(itemStack))
                {
                    list.Add(itemStack);
                }
            }

            return list.ToArray();
        }

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return EnumIgniteState.NotIgnitable;
        }

        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBloomery).CanIgnite())
            {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }

            if (!(secondsIgniting > 4f))
            {
                return EnumIgniteState.Ignitable;
            }

            return EnumIgniteState.IgniteNow;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            (byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBloomery)?.TryIgnite();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (itemstack != null && itemstack.Class == EnumItemClass.Block && itemstack.Collectible.Code.Path.StartsWith("betterbloomerychimney"))
            {
                if (world.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).IsReplacableBy(itemstack.Block))
                {
                    itemstack.Block.DoPlaceBlock(world, byPlayer, new BlockSelection
                    {
                        Position = blockSel.Position.UpCopy(),
                        Face = BlockFacing.UP
                    }, itemstack);
                    world.PlaySoundAt(Sounds?.Place, blockSel.Position.X, blockSel.Position.Y + 1, blockSel.Position.Z, byPlayer, randomizePitch: true, 16f);
                    if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                    {
                        byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                    }
                }

                return true;
            }

            BlockEntityBloomery blockEntityBloomery = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBloomery;
            if (blockEntityBloomery != null)
            {
                if (itemstack == null)
                {
                    return true;
                }

                if (blockEntityBloomery.TryAdd(byPlayer, (!byPlayer.Entity.Controls.CtrlKey) ? 1 : 5) && world.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(pos.UpCopy());
            if (block.Code.Path == "betterbloomerychimney" + "-" + block.Code.EndVariant())
            {
                block.OnBlockBroken(world, pos.UpCopy(), byPlayer, dropQuantityMultiplier);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            List<ItemStack> list = new List<ItemStack>();
            for (int i = 0; i < Drops.Length; i++)
            {
                if (Drops[i].Tool.HasValue && (byPlayer == null || Drops[i].Tool != byPlayer.InventoryManager.ActiveTool))
                {
                    continue;
                }

                ItemStack nextItemStack = Drops[i].GetNextItemStack(dropQuantityMultiplier);
                if (nextItemStack != null)
                {
                    list.Add(nextItemStack);
                    if (Drops[i].LastDrop)
                    {
                        break;
                    }
                }
            }

            return list.ToArray();
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
