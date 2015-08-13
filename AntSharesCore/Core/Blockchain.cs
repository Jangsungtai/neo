﻿using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Caching;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Core
{
    public class Blockchain : IDisposable
    {
        public const int SecondsPerBlock = 15;
        private const int BlocksPerYear = 365 * 24 * 60 * 60 / SecondsPerBlock;
        private const double R_Init = 0.5;
        private const double R_Final = 0.3;
        public static readonly decimal GenerationFactor = 1 - (decimal)Math.Pow(R_Final / R_Init, 1.0 / BlocksPerYear);
        //TODO: 备用矿工未来要有5-7个
        public static readonly ECCPublicKey[] StandbyMiners =
        {
            "02c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd0".HexToBytes().ToPublicKey()
        };
        public static readonly Block GenesisBlock = "000000000000000000000000000000000000000000000000000000000000000000000000e7b746f8496dd080ca30c40186fddfe30c7d61209842197eef11fee0852cc25c375ecc55000000001dac2b7c00000000eea34400951bc0e31a530ce8a8a63485c6271147674065e19b1bd7f90c1fb26182bce24f0c840235f647efd83647988661736500546a89bb710d832bf487a26c1053c8ccb6b6fb30be9d57ccb39bd7fb098f6e1d593025512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae0200000000000000004000455b7b276c616e67273a277a682d434e272c276e616d65273a27e5b08fe89a81e882a1277d2c7b276c616e67273a27656e272c276e616d65273a27416e745368617265277d5d0000c16ff2862300eea34400951bc0e31a530ce8a8a63485c6271147eea34400951bc0e31a530ce8a8a63485c62711470000016740da12d7b9a3f66d3a27a160e73ffd3fdbd712eedc3262913ec93944f874ef823d2ab972ac06616c0481afd1eb9fecc379b4030c3212641f035d8ed4095ea7476a25512102c4a2fd44a0d80d84ea3258eaf7c3c2c9f5d22369dbbe5dafdcf4ead89f7fbdd051ae".HexToBytes().AsSerializable<Block>();
        public static readonly RegisterTransaction AntShare = (RegisterTransaction)GenesisBlock.Transactions[1];
        public static readonly RegisterTransaction AntCoin = new RegisterTransaction
        {
            AssetType = AssetType.AntCoin,
            Name = "[{'lang':'zh-CN','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
            Amount = Fixed8.FromDecimal(100000000),
            Issuer = new UInt160(),
            Admin = new UInt160(),
            Inputs = new TransactionInput[0],
            Outputs = new TransactionOutput[0],
            Scripts = new byte[0][]
        };

        //TODO: 是否应该根据内存大小来优化缓存容量？
        private static BlockCache cache = new BlockCache(5760);

        public virtual BlockchainAbility Ability => BlockchainAbility.None;
        public virtual UInt256 CurrentBlockHash => GenesisBlock.Hash;
        public static Blockchain Default { get; private set; } = new Blockchain();
        public virtual uint Height => 0;
        public virtual bool IsReadOnly => true;

        protected Blockchain()
        {
            LocalNode.NewBlock += LocalNode_NewBlock;
        }

        public virtual bool ContainsAsset(UInt256 hash)
        {
            return hash == AntCoin.Hash || hash == AntShare.Hash;
        }

        public virtual bool ContainsBlock(UInt256 hash)
        {
            return hash == GenesisBlock.Hash || cache.Contains(hash);
        }

        public virtual bool ContainsTransaction(UInt256 hash)
        {
            return hash == AntCoin.Hash || GenesisBlock.Transactions.Any(p => p.Hash == hash);
        }

        public bool ContainsUnspent(TransactionInput input)
        {
            return ContainsUnspent(input.PrevTxId, input.PrevIndex);
        }

        public virtual bool ContainsUnspent(UInt256 hash, ushort index)
        {
            throw new NotSupportedException();
        }

        public virtual void Dispose()
        {
            LocalNode.NewBlock -= LocalNode_NewBlock;
        }

        public virtual IEnumerable<RegisterTransaction> GetAssets()
        {
            throw new NotSupportedException();
        }

        public virtual Block GetBlock(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            lock (cache.SyncRoot)
            {
                if (cache.Contains(hash))
                    return cache[hash];
            }
            return null;
        }

        public virtual Block GetBlockAndHeight(UInt256 hash, out uint height)
        {
            height = 0;
            if (hash == GenesisBlock.Hash)
                return GenesisBlock;
            return null;
        }

        public virtual int GetBlockHeight(UInt256 hash)
        {
            if (hash == GenesisBlock.Hash) return 0;
            return -1;
        }

        public IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            return GetEnrollments(Enumerable.Empty<Transaction>());
        }

        public virtual IEnumerable<EnrollmentTransaction> GetEnrollments(IEnumerable<Transaction> others)
        {
            throw new NotSupportedException();
        }

        public virtual BlockHeader GetHeader(UInt256 hash)
        {
            return GetBlock(hash)?.Header;
        }

        public static int GetMinSignatureCount(int miner_count)
        {
            return miner_count / 2 + 1;
        }

        public virtual Block GetNextBlock(UInt256 hash)
        {
            return null;
        }

        public virtual UInt256 GetNextBlockHash(UInt256 hash)
        {
            return null;
        }

        public virtual Fixed8 GetQuantityIssued(UInt256 asset_id)
        {
            throw new NotSupportedException();
        }

        public virtual Transaction GetTransaction(UInt256 hash)
        {
            if (hash == AntCoin.Hash)
                return AntCoin;
            return GenesisBlock.Transactions.FirstOrDefault(p => p.Hash == hash);
        }

        public virtual TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            throw new NotSupportedException();
        }

        public virtual IEnumerable<TransactionOutput> GetUnspentAntShares()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<Vote> GetVotes()
        {
            return GetVotes(Enumerable.Empty<Transaction>());
        }

        public virtual IEnumerable<Vote> GetVotes(IEnumerable<Transaction> others)
        {
            throw new NotSupportedException();
        }

        public virtual bool IsDoubleSpend(Transaction tx)
        {
            throw new NotSupportedException();
        }

        private void LocalNode_NewBlock(object sender, Block block)
        {
            OnBlock(block);
        }

        protected virtual void OnBlock(Block block)
        {
            cache.Add(block);
        }

        public static void RegisterBlockchain(Blockchain blockchain)
        {
            if (blockchain == null) throw new ArgumentNullException();
            if (Default != null)
                Default.Dispose();
            Default = blockchain;
        }
    }
}