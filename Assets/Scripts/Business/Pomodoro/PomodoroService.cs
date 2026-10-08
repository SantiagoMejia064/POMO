using System;
using System.Collections.Generic;
using System.Linq;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.Shared.Enums;

namespace Pomo.Business.Pomodoro
{
    /// <summary>
    /// Defines the duration options used by the Pomodoro timer and persists the
    /// user's custom blocks, favourites and last selected block.
    /// </summary>
    public class PomodoroService
    {
        public const string ShortBlockId = "preset-short";
        public const string MediumBlockId = "preset-medium";
        public const string LongBlockId = "preset-long";

        private readonly WorkBlockRepository workBlockRepository;

        public PomodoroService()
            : this(new WorkBlockRepository())
        {
        }

        public PomodoroService(WorkBlockRepository workBlockRepository)
        {
            this.workBlockRepository = workBlockRepository ?? throw new ArgumentNullException(nameof(workBlockRepository));
        }

        public IReadOnlyList<WorkBlockModel> GetAvailableBlocks()
        {
            List<WorkBlockModel> blocks = CreatePresetBlocks();

            foreach (WorkBlockModel customBlock in workBlockRepository.GetCustomBlocks())
            {
                if (customBlock != null)
                {
                    blocks.Add(Clone(customBlock));
                }
            }

            return blocks.AsReadOnly();
        }

        public IReadOnlyList<WorkBlockModel> GetFavoriteBlocks()
        {
            List<WorkBlockModel> favoriteBlocks = new List<WorkBlockModel>();

            foreach (WorkBlockModel block in GetAvailableBlocks())
            {
                if (block.isFavorite)
                {
                    favoriteBlocks.Add(block);
                }
            }

            return favoriteBlocks.AsReadOnly();
        }

        public WorkBlockModel GetSelectedBlock()
        {
            WorkBlockModel selectedBlock = GetBlock(workBlockRepository.GetSelectedBlockId());
            return selectedBlock ?? GetBlock(ShortBlockId);
        }

        public WorkBlockModel GetBlock(string blockId)
        {
            if (string.IsNullOrWhiteSpace(blockId))
            {
                return null;
            }

            foreach (WorkBlockModel preset in CreatePresetBlocks())
            {
                if (preset.id == blockId)
                {
                    return preset;
                }
            }

            WorkBlockModel customBlock = workBlockRepository.GetCustomBlockById(blockId);
            return customBlock == null ? null : Clone(customBlock);
        }

        public bool SelectBlock(string blockId)
        {
            if (GetBlock(blockId) == null)
            {
                return false;
            }

            workBlockRepository.SetSelectedBlockId(blockId);
            return true;
        }

        public WorkBlockValidationResult ValidateCustomBlock(string name, int focusMinutes, int breakMinutes)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return WorkBlockValidationResult.EmptyName;
            }

            if (focusMinutes < 1 || focusMinutes > 180)
            {
                return WorkBlockValidationResult.InvalidFocusDuration;
            }

            if (breakMinutes < 0 || breakMinutes > 60)
            {
                return WorkBlockValidationResult.InvalidBreakDuration;
            }

            string normalizedName = name.Trim();

            foreach (WorkBlockModel block in GetAvailableBlocks())
            {
                if (string.Equals(block.name, normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return WorkBlockValidationResult.DuplicateName;
                }
            }

            return WorkBlockValidationResult.Valid;
        }

        public WorkBlockModel CreateCustomBlock(string name, int focusMinutes, int breakMinutes)
        {
            if (ValidateCustomBlock(name, focusMinutes, breakMinutes) != WorkBlockValidationResult.Valid)
            {
                return null;
            }

            WorkBlockModel customBlock = new WorkBlockModel
            {
                id = Guid.NewGuid().ToString(),
                name = name.Trim(),
                focusMinutes = focusMinutes,
                breakMinutes = breakMinutes,
                isCustom = true,
                isFavorite = false
            };

            workBlockRepository.AddCustomBlock(customBlock);
            return Clone(customBlock);
        }

        public bool SetFavorite(string blockId, bool isFavorite)
        {
            WorkBlockModel preset = GetPresetBlock(blockId);

            if (preset != null)
            {
                workBlockRepository.SetPresetFavorite(blockId, isFavorite);
                return true;
            }

            WorkBlockModel customBlock = workBlockRepository.GetCustomBlockById(blockId);

            if (customBlock == null)
            {
                return false;
            }

            customBlock.isFavorite = isFavorite;
            return workBlockRepository.UpdateCustomBlock(customBlock);
        }

        private List<WorkBlockModel> CreatePresetBlocks()
        {
            IReadOnlyList<string> favoritePresetIds = workBlockRepository.GetFavoritePresetIds();

            return new List<WorkBlockModel>
            {
                CreatePreset(ShortBlockId, "Corta", 25, 5, favoritePresetIds.Contains(ShortBlockId)),
                CreatePreset(MediumBlockId, "Media", 50, 10, favoritePresetIds.Contains(MediumBlockId)),
                CreatePreset(LongBlockId, "Larga", 90, 15, favoritePresetIds.Contains(LongBlockId))
            };
        }

        private WorkBlockModel GetPresetBlock(string blockId)
        {
            foreach (WorkBlockModel preset in CreatePresetBlocks())
            {
                if (preset.id == blockId)
                {
                    return preset;
                }
            }

            return null;
        }

        private static WorkBlockModel CreatePreset(string id, string name, int focusMinutes, int breakMinutes, bool isFavorite)
        {
            return new WorkBlockModel
            {
                id = id,
                name = name,
                focusMinutes = focusMinutes,
                breakMinutes = breakMinutes,
                isCustom = false,
                isFavorite = isFavorite
            };
        }

        private static WorkBlockModel Clone(WorkBlockModel block)
        {
            return new WorkBlockModel
            {
                id = block.id,
                name = block.name,
                focusMinutes = block.focusMinutes,
                breakMinutes = block.breakMinutes,
                isCustom = block.isCustom,
                isFavorite = block.isFavorite
            };
        }
    }
}
