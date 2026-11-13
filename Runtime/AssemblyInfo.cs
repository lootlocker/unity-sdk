using System.Runtime.CompilerServices;

// The edit-mode test assembly needs access to the internal config resolution helpers so they can
// be tested without going through the public API or touching the real Resources/Config folder.
[assembly: InternalsVisibleTo("LootLockerEditModeTests")]
