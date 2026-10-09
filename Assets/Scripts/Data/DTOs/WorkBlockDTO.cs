using System;
using System.Collections.Generic;
using Pomo.Data.Models;

namespace Pomo.Data.DTOs
{
    [Serializable]
    public class WorkBlockDTO
    {
        public string selectedBlockId = "preset-short";
        public List<string> favoritePresetIds = new List<string>();
        public bool defaultPresetFavoritesInitialized;
        public List<WorkBlockModel> customBlocks = new List<WorkBlockModel>();
    }
}
