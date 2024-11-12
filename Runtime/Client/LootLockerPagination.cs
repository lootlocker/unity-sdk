namespace LootLocker
{
    public class LootLockerPaginationResponse<TKey>
    {
        /// <summary>
        /// The total available items in this list
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// The cursor that points to the next item in the list. Use this in subsequent requests to get additional items from the list.
        /// </summary>
        public TKey next_cursor { get; set; }
        /// <summary>
        /// The cursor that points to the first item in this batch of items.
        /// </summary>
        public TKey previous_cursor { get; set; }
    }

    public class LootLockerExtendedPaginationError
    {
        /// <summary>
        /// Which field in the pagination that this error relates to
        /// </summary>
        public string field { get; set; }
        /// <summary>
        /// The error message in question
        /// </summary>
        public string message { get; set; }
    }

    public class LootLockerExtendedPagination
    {
        /// <summary>
        /// How many entries in total exists in the paginated list
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// How many entries (counting from the beginning of the paginated list) from the first entry that the current page starts at
        /// </summary>
        public int offset { get; set; }
        /// <summary>
        /// Number of entries on each page
        /// </summary>
        public int per_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the last page of entries
        /// </summary>
        public int last_page { get; set; }
        /// <summary>
        /// The page index used for fetching this page of entries
        /// </summary>
        public int current_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the page of entries immediately succeeding this page of entries
        /// </summary>
        public int? next_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the page of entries immediately preceding this page of entries
        /// </summary>
        public int? prev_page { get; set; }
        /// <summary>
        /// List of pagination errors (if any). These are errors specifically related to the pagination of the entry set.
        /// </summary>
        public LootLockerExtendedPaginationError[] errors { get; set; }
    }
    }
