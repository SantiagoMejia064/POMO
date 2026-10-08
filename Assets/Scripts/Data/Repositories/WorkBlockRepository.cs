using System;
using System.Collections.Generic;
using System.IO;
using Pomo.Data.DTOs;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.Data.Repositories
{
    public class WorkBlockRepository
    {
        private readonly JsonStorageService storageService;
        private WorkBlockDTO storedBlocks;

        public WorkBlockRepository()
            : this(new JsonStorageService(Path.Combine(Application.persistentDataPath, AppConstants.WorkBlocksFileName)))
        {
        }

        public WorkBlockRepository(JsonStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            Reload();
        }

        public IReadOnlyList<WorkBlockModel> GetCustomBlocks()
        {
            return storedBlocks.customBlocks.AsReadOnly();
        }

        public IReadOnlyList<string> GetFavoritePresetIds()
        {
            return storedBlocks.favoritePresetIds.AsReadOnly();
        }

        public string GetSelectedBlockId()
        {
            return storedBlocks.selectedBlockId;
        }

        public WorkBlockModel GetCustomBlockById(string blockId)
        {
            return string.IsNullOrWhiteSpace(blockId)
                ? null
                : storedBlocks.customBlocks.Find(block => block != null && block.id == blockId);
        }

        public void AddCustomBlock(WorkBlockModel block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            storedBlocks.customBlocks.Add(block);
            SaveChanges();
        }

        public bool UpdateCustomBlock(WorkBlockModel block)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.id))
            {
                return false;
            }

            int blockIndex = storedBlocks.customBlocks.FindIndex(storedBlock =>
                storedBlock != null && storedBlock.id == block.id);

            if (blockIndex < 0)
            {
                return false;
            }

            storedBlocks.customBlocks[blockIndex] = block;
            SaveChanges();
            return true;
        }

        public void SetSelectedBlockId(string blockId)
        {
            storedBlocks.selectedBlockId = blockId;
            SaveChanges();
        }

        public void SetPresetFavorite(string blockId, bool isFavorite)
        {
            bool alreadyFavorite = storedBlocks.favoritePresetIds.Contains(blockId);

            if (isFavorite && !alreadyFavorite)
            {
                storedBlocks.favoritePresetIds.Add(blockId);
            }
            else if (!isFavorite && alreadyFavorite)
            {
                storedBlocks.favoritePresetIds.Remove(blockId);
            }

            SaveChanges();
        }

        public void Reload()
        {
            if (!storageService.TryLoad(out storedBlocks) || storedBlocks == null)
            {
                storedBlocks = new WorkBlockDTO();
            }

            storedBlocks.favoritePresetIds ??= new List<string>();
            storedBlocks.customBlocks ??= new List<WorkBlockModel>();

            if (string.IsNullOrWhiteSpace(storedBlocks.selectedBlockId))
            {
                storedBlocks.selectedBlockId = "preset-short";
                SaveChanges();
            }
        }

        private void SaveChanges()
        {
            storageService.Save(storedBlocks);
        }
    }
}
