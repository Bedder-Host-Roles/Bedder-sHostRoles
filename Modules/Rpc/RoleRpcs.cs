using Hazel;
using InnerNet;
using System.Collections.Generic;

namespace BHR.Modules.Rpc
{
    public static class RoleRpcs
    {
        private static CustomRpcSender StartRoleRpc(string rpcName, uint netId, CustomRPC rpcType)
        {
            return CustomRpcSender.Create(rpcName, SendOption.Reliable).AutoStartRpc(netId, (byte)rpcType);
        }

        private static void EndAndSend(CustomRpcSender sender)
        {
            sender.EndRpc();
            sender.SendMessage();
        }

        public static void SendGuessKill(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.GuessKill), netId, CustomRPC.GuessKill);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        // public static void SendProsecutor(uint netId, byte playerId)
        // {
        //     var sender = StartRoleRpc(nameof(CustomRPC.Judge), netId, CustomRPC.Judge);
        //     sender.stream.Write(playerId);
        //     EndAndSend(sender);
        // }

        public static void SendCouncillorJudge(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.CouncillorJudge), netId, CustomRPC.CouncillorJudge);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendNemesisRevenge(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.NemesisRevenge), netId, CustomRPC.NemesisRevenge);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendRetributionistRevenge(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.RetributionistRevenge), netId, CustomRPC.RetributionistRevenge);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendSetBountyTarget(uint netId, byte bountyId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetBountyTarget), netId, CustomRPC.SetBountyTarget);
            sender.stream.Write(bountyId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSyncPuppet(uint netId, byte typeId, byte puppetId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SyncPuppet), netId, CustomRPC.SyncPuppet);
            sender.stream.Write(typeId);
            sender.stream.Write(puppetId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSetKillOrSpell(uint netId, byte playerId, bool spellMode)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetKillOrSpell), netId, CustomRPC.SetKillOrSpell);
            sender.stream.Write(playerId);
            sender.stream.Write(spellMode);
            EndAndSend(sender);
        }

        public static void SendSetDousedPlayer(uint netId, byte playerId, byte targetId, bool isDoused)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetDousedPlayer), netId, CustomRPC.SetDousedPlayer);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            sender.stream.Write(isDoused);
            EndAndSend(sender);
        }

        public static void SendDoSpell(uint netId, byte witchId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.DoSpell), netId, CustomRPC.DoSpell);
            sender.stream.Write(witchId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendDoHex(uint netId, byte hexId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.DoHex), netId, CustomRPC.DoHex);
            sender.stream.Write(hexId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSniperSync(uint netId, byte playerId, List<byte> snList)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SniperSync), netId, CustomRPC.SniperSync);
            sender.stream.Write(playerId);
            sender.stream.Write(snList.Count);
            foreach (var sn in snList)
            {
                sender.stream.Write(sn);
            }
            EndAndSend(sender);
        }

        public static void SendSetLoverPairs(uint netId, int pairCount, List<(byte, byte)> loverPairs, byte loverless)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetLoverPairs), netId, CustomRPC.SetLoverPairs);
            sender.stream.Write(pairCount);
            foreach (var pair in loverPairs)
            {
                sender.stream.Write(pair.Item1);
                sender.stream.Write(pair.Item2);
            }
            sender.stream.Write(loverless);
            EndAndSend(sender);
        }

        public static void SendFireworkerState(uint netId, byte playerId, int nowFireworkerCount, int state)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SendFireworkerState), netId, CustomRPC.SendFireworkerState);
            sender.stream.Write(playerId);
            sender.stream.Write(nowFireworkerCount);
            sender.stream.Write(state);
            EndAndSend(sender);
        }

        public static void SendSetCurrentDousingTarget(uint netId, byte arsonistId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetCurrentDousingTarget), netId, CustomRPC.SetCurrentDousingTarget);
            sender.stream.Write(arsonistId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSetEvilTrackerTarget(uint netId, byte trackerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetEvilTrackerTarget), netId, CustomRPC.SetEvilTrackerTarget);
            sender.stream.Write(trackerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSetDrawPlayer(uint netId, byte playerId, byte targetId, bool isDrawed)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetDrawPlayer), netId, CustomRPC.SetDrawPlayer);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            sender.stream.Write(isDrawed);
            EndAndSend(sender);
        }

        public static void SendSetCrewpostorTasksDone(uint netId, byte playerId, int tasksDone)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetCrewpostorTasksDone), netId, CustomRPC.SetCrewpostorTasksDone);
            sender.stream.Write(playerId);
            sender.stream.WritePacked(tasksDone);
            EndAndSend(sender);
        }

        public static void SendSetCurrentDrawTarget(uint netId, byte revoId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetCurrentDrawTarget), netId, CustomRPC.SetCurrentDrawTarget);
            sender.stream.Write(revoId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSyncJailerData(uint netId, byte playerId, int jailerTarget, bool hasExe, bool didVote)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SyncJailerData), netId, CustomRPC.SyncJailerData);
            sender.stream.Write(playerId);
            sender.stream.WritePacked(jailerTarget);
            sender.stream.Write(hasExe);
            sender.stream.Write(didVote);
            EndAndSend(sender);
        }

        public static void SendSetInspectorLimit(uint netId, byte playerId, int limit)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetInspectorLimit), netId, CustomRPC.SetInspectorLimit);
            sender.stream.Write(playerId);
            sender.stream.WritePacked(limit);
            EndAndSend(sender);
        }

        public static void SendKeeper(uint netId, int type, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.KeeperRPC), netId, CustomRPC.KeeperRPC);
            sender.stream.Write(type);
            if (type == 0)
            {
                sender.stream.Write(playerId);
                sender.stream.Write(targetId);
            }
            EndAndSend(sender);
        }

        public static void SendSetAlchemistTimer(uint netId, bool fixSabo, byte potionId, string invisTime)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetAlchemistTimer), netId, CustomRPC.SetAlchemistTimer);
            sender.stream.Write(fixSabo);
            sender.stream.Write(potionId);
            sender.stream.Write(invisTime);
            EndAndSend(sender);
        }

        public static void SendUndertakerLocationSync(uint netId, byte playerId, float xLoc, float yLoc)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.UndertakerLocationSync), netId, CustomRPC.UndertakerLocationSync);
            sender.stream.Write(playerId);
            sender.stream.Write(xLoc);
            sender.stream.Write(yLoc);
            EndAndSend(sender);
        }

        public static void SendLightningSetGhostPlayer(uint netId, byte playerId, bool isGhost)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.LightningSetGhostPlayer), netId, CustomRPC.LightningSetGhostPlayer);
            sender.stream.Write(playerId);
            sender.stream.Write(isGhost);
            EndAndSend(sender);
        }

        public static void SendSetConsigliere(uint netId, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetConsigliere), netId, CustomRPC.SetConsigliere);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSetGreedy(uint netId, byte playerId, bool isOdd)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetGreedy), netId, CustomRPC.SetGreedy);
            sender.stream.Write(playerId);
            sender.stream.Write(isOdd);
            EndAndSend(sender);
        }

        public static void SendSetInquisitor(uint netId, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetInquisitor), netId, CustomRPC.SetInquisitor);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendBenefactor(uint netId, int type, byte playerId, int taskIndex, byte targetId, string shieldedPlayers)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.BenefactorRPC), netId, CustomRPC.BenefactorRPC);
            sender.stream.Write(type);
            if (type == 0)
            {
                sender.stream.Write(playerId);
            }
            if (type == 2)
            {
                sender.stream.Write(playerId);
                sender.stream.Write(taskIndex);
            }
            if (type == 3)
            {
                sender.stream.Write(playerId);
                sender.stream.Write(taskIndex);
                sender.stream.Write(targetId);
                sender.stream.Write(shieldedPlayers);
            }
            if (type == 4)
            {
                sender.stream.Write(targetId);
            }
            EndAndSend(sender);
        }

        public static void SendSetSwapperVotes(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetSwapperVotes), netId, CustomRPC.SetSwapperVotes);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendSetMarkedPlayer(uint netId, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetMarkedPlayer), netId, CustomRPC.SetMarkedPlayer);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendPresidentEnd(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.PresidentEnd), netId, CustomRPC.PresidentEnd);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendPresidentReveal(uint netId, byte playerId, bool checkReveal)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.PresidentReveal), netId, CustomRPC.PresidentReveal);
            sender.stream.Write(playerId);
            sender.stream.Write(checkReveal);
            EndAndSend(sender);
        }

        public static void SendSetInvestigatorLimit(uint netId, bool setTarget, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetInvestgatorLimit), netId, CustomRPC.SetInvestgatorLimit);
            sender.stream.Write(setTarget);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendSetOverseerRevealedPlayer(uint netId, byte playerId, byte targetId, bool isRevealed)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetOverseerRevealedPlayer), netId, CustomRPC.SetOverseerRevealedPlayer);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            sender.stream.Write(isRevealed);
            EndAndSend(sender);
        }

        public static void SendSetOverseerTimer(uint netId, byte type, byte playerId, PlayerControl target, float timer)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetOverseerTimer), netId, CustomRPC.SetOverseerTimer);
            sender.stream.Write(type);
            sender.stream.Write(playerId);
            if (target != null && type == 1)
            {
                sender.stream.WriteNetObject(target);
                sender.stream.Write(timer);
            }
            EndAndSend(sender);
        }

        public static void SendSetChameleonTimer(uint netId, byte playerId, string invisCooldown, string invisDuration)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SetChameleonTimer), netId, CustomRPC.SetChameleonTimer);
            sender.stream.Write(playerId);
            sender.stream.Write(invisCooldown);
            sender.stream.Write(invisDuration);
            EndAndSend(sender);
        }

        public static void SendSyncAdmiredList(uint netId, byte playerId, byte targetId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.SyncAdmiredList), netId, CustomRPC.SyncAdmiredList);
            sender.stream.Write(playerId);
            sender.stream.Write(targetId);
            EndAndSend(sender);
        }

        public static void SendDictator(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.DictatorRPC), netId, CustomRPC.DictatorRPC);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendNecronomicon(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.Necronomicon), netId, CustomRPC.Necronomicon);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendExorcistExorcise(uint netId, byte playerId)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.ExorcistExorcise), netId, CustomRPC.ExorcistExorcise);
            sender.stream.Write(playerId);
            EndAndSend(sender);
        }

        public static void SendGuess(uint netId, byte playerId, CustomRoles role)
        {
            var sender = StartRoleRpc(nameof(CustomRPC.Guess), netId, CustomRPC.Guess);
            sender.stream.Write(playerId);
            sender.stream.WritePacked((int)role);
            EndAndSend(sender);
        }
    }
}
