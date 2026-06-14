# Recording & Playback Commands

The bridge supports recording gameplay snapshots to JSONL files and playing them back for analysis.

## Recording

### record:start[:interval]

Start recording gameplay snapshots.

**Request:** `record:start` or `record:start:100` (100ms interval)

**Response:** `{status, file, intervalMs}`

Each frame captures player state and entities with adaptive depth:
- Unique/Rare entities get full component dumps
- Normal/Magic entities get lightweight data

Default interval is 200ms.

### record:stop

Stop recording and flush the file.

**Request:** `record:stop`

**Response:** `{status, frames, durationMs, file, sizeBytes}`

### record:status

Check if recording is active.

**Request:** `record:status`

**Response:** `{status, isRecording, frames?, durationMs?, file?, intervalMs?}`

### snapshot

Capture a single frame. Also appends to any active recording.

**Request:** `snapshot`

**Response:** Full snapshot JSON (same format as a recording frame).

## Playback

Recording files are stored in `claude-bridge\recordings\`.

### recording:list

List saved recording files.

**Request:** `recording:list`

**Response:** `{recordings: [{name, sizeBytes, modified}, ...]}`

### recording:load:filename

Load a recording for playback analysis.

**Request:** `recording:load:rec_20260319_143000.jsonl`

**Response:** `{status, file, frames}`

### recording:frame:N

Read a specific frame from the loaded recording.

**Request:** `recording:frame:0`

**Response:** The frame's snapshot JSON.

### recording:range:N:M

Read a range of frames from the loaded recording.

**Request:** `recording:range:0:10`

**Response:** `{frames: [...], startFrame, endFrame}`

### recording:search:term

Search loaded recording for frames containing a text match.

**Request:** `recording:search:Strongbox`

**Response:** `{term, matchingFrames: [{frameIndex, preview}, ...], totalSearched}`

### recording:summary

Analyze the loaded recording: entity counts, buff timelines.

**Request:** `recording:summary`

**Response:** `{totalFrames, durationMs, entitySummary, buffTimeline, ...}`

## Usage Examples

**Record a boss fight:**
1. `record:start:100` -- start at 100ms intervals for high fidelity
2. Fight the boss
3. `record:stop` -- stop and save
4. `recording:list` -- find the file
5. `recording:load:<filename>` -- load it
6. `recording:summary` -- get overview
7. `recording:search:Boss` -- find frames with the boss entity

**Quick snapshot for debugging:**
1. `snapshot` -- capture current state
2. Analyze the response JSON for the issue
