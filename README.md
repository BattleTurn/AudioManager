# Audio Manager

README này mô tả cách setup và sử dụng package `BattleTurn Audio Manager` trong project Unity.

## 1. Package này dùng để làm gì

Package quản lý audio theo cấu trúc:

- `AudioDataManagerSO`: asset gốc, chứa toàn bộ album audio.
- `AudioAlbumSO`: một album lớn, ví dụ `SFX`, `MFX`.
- `AudioCategorySO`: nhóm audio con bên trong album, ví dụ `UI`, `Weapon`, `BGM`.
- `AudioContentSO`: từng clip audio riêng lẻ.
- `SoundManager` và `MusicManager`: runtime manager để phát SFX và Music.
- `AudioPlayer`: component có dropdown để chọn audio ngay trong Inspector.

Package cũng tự generate:

- `GameMixer.mixer`
- `AudioType.cs`
- `AudioNames.cs`
- `AudioMixerExposedParameter.cs`
- `AudioMixerGroupName.cs`

Các file generated nằm trong thư mục:

```text
Assets/Plugins/BattleTurn/Generated/AudioManager
```

## 2. Cài đặt ban đầu

Sau khi thêm package vào project:

1. Mở Unity và đợi compile xong.
2. Nếu hiện popup `AudioManager Extra`, import file `.unitypackage` được yêu cầu.
3. Nếu popup không hiện nhưng bạn vẫn cần import lại, dùng menu:

```text
Audio/Import Extra Package
```

Lần đầu mở project, package sẽ cố gắng tự tạo `GameMixer` và auto-wire mixer vào `AudioDataManagerSO` nếu asset đã tồn tại.

## 3. Quy trình setup chuẩn

Nên làm theo đúng thứ tự sau.

### Bước 1: Tạo AudioContent

Tạo asset tại menu:

```text
BattleTurn/Audio/AudioContent
```

Mỗi `AudioContentSO` gồm:

- `_name`: tên key dùng để gọi audio trong code.
- `_clip`: `AudioClip` thực tế.

Khuyến nghị:

- Tên không có khoảng trắng.
- Tên phải unique trong cùng category.
- Dùng tên ổn định vì code generated phụ thuộc vào tên này.

### Bước 2: Tạo Category

Tạo asset tại menu:

```text
BattleTurn/Audio/AudioCategorySO
```

Trong category:

- Đặt `_name` cho category.
- Gán danh sách `_audioContents`.

Ví dụ category:

- `UI`
- `Footstep`
- `Weapon`
- `BGM`

### Bước 3: Tạo Album

Tạo asset tại menu:

```text
BattleTurn/Audio/AudioAlbumSO
```

Thông thường bạn sẽ tạo tối thiểu 2 album:

- `SFX`
- `MFX`

Trong mỗi album:

- Đặt `_name` đúng tên album.
- Gán `_audioCategories`.

Lưu ý quan trọng:

- `SoundManager` đọc album có tên `SFX`.
- `MusicManager` đọc album có tên `MFX`.

Nếu bạn đổi tên 2 album này sang tên khác, 2 manager mặc định sẽ không map đúng dữ liệu.

### Bước 4: Tạo AudioDataManagerSO

Tạo asset tại menu:

```text
BattleTurn/Audio/AudioDataManagerSO
```

Trong asset này:

- Gán `_audioDatas` = danh sách album, thường là `SFX` và `MFX`.

### Bước 5: Tạo hoặc cập nhật mixer

Dùng menu:

```text
Tools/Audio/Create Game Mixer
```

Menu này sẽ:

- tạo `GameMixer.mixer`
- auto-wire mixer vào các `AudioDataManagerSO`
- generate `AudioMixerExposedParameter.cs`
- generate `AudioMixerGroupName.cs`

Khi đã có mixer rồi, nếu chỉ muốn cập nhật generated file thì dùng:

```text
Tools/Audio/Update Mixer Exposed Parameter
Tools/Audio/Update Mixer Group Names
Tools/Audio/Auto Wire AudioDataManager AudioMixer
```

### Bước 6: Build generated code cho AudioData

Chọn asset `AudioDataManagerSO` trong Inspector, sau đó bấm nút:

```text
Build AudioData
```

Thao tác này sẽ generate lại:

- `AudioType.cs`
- `AudioNames.cs`

Bạn cần bấm lại `Build AudioData` mỗi khi:

- thêm album mới
- đổi tên album
- thêm category
- đổi tên category
- thêm audio content
- đổi tên audio content

## 4. Setup trong Scene

Project hiện tại đang dùng 2 manager runtime:

- `SoundManager`
- `MusicManager`

Bạn nên đặt sẵn 2 object này trong bootstrap scene hoặc scene đầu tiên, rồi assign cùng một `AudioDataManagerSO` vào field `audioManager`.

Lý do:

- Nếu không có manager trong scene, singleton sẽ tự tạo `GameObject` mới.
- Nhưng object được tạo runtime đó không tự có reference tới `AudioDataManagerSO`.
- Khi đó audio có thể không phát đúng hoặc bị lỗi null/reference thiếu data.

Khuyến nghị:

1. Tạo một GameObject `SoundManager` và add component `SoundManager`.
2. Tạo một GameObject `MusicManager` và add component `MusicManager`.
3. Gán cùng asset `AudioDataManagerSO` cho cả hai.
4. Để các object này ở scene đầu tiên của game.

Hai manager này đã gọi `DontDestroyOnLoad`, nên chỉ cần tạo một lần.

## 5. Phát audio bằng code

Namespace chính:

```csharp
using BattleTurn.AudioManager.Runtime;
using BattleTurn.AudioManager.Runtime.Implemented;
```

### Phát SFX cơ bản

```csharp
SoundManager.Instance.Play("Click");
```

### Phát Music cơ bản

```csharp
MusicManager.Instance.Play("MainTheme");
```

### Phát theo category + audio name

```csharp
SoundManager.Instance.Play("UI", "Click", null);
MusicManager.Instance.Play("BGM", "MainTheme", null);
```

### Phát one-shot

```csharp
SoundManager.Instance.PlayOneShot("Explosion");
```

### Phát tại vị trí world

```csharp
SoundManager.Instance.PlayAt("Explosion", hitPoint);
```

### Phát bám theo transform

```csharp
SoundManager.Instance.PlayFollow("EngineLoop", targetTransform);
```

### Dùng tên generated thay vì hard-code string

```csharp
SoundManager.Instance.Play(
    AudioNames.SFX.Categories.UI,
    AudioNames.SFX.UI.CLICK,
    null);
```

Nếu tên generated chưa đúng hoặc chưa xuất hiện, hãy chọn `AudioDataManagerSO` và bấm lại `Build AudioData`.

## 6. Playback parameter

Manager hỗ trợ truyền thêm các parameter phát audio qua `IParameterizable`.

Ví dụ:

```csharp
var parameters = new IParameterizable[]
{
    new DelayParameter(0.25f),
    new LoopParameter(1),
};

SoundManager.Instance.Play("UI", "Click", parameters);
```

Các parameter hiện có:

- `OneShotParameter`: phát bằng `PlayOneShot`.
- `WorldPositionParameter`: đặt vị trí thế giới cho `AudioSource`.
- `FollowParameter`: gắn `AudioSource` theo `Transform`.
- `DelayParameter`: delay trước khi phát.
- `LoopParameter`: lặp số lần hoặc vô hạn.

Ghi chú `LoopParameter`:

- `0`: phát 1 lần.
- `1`: phát tổng cộng 2 lần.
- `-1`: loop vô hạn.

Ghi chú `DelayParameter`:

- `DelayType.OnStart`: delay ở lần phát đầu.
- `DelayType.EveryLoop`: delay ở mọi vòng lặp.

## 7. Điều chỉnh mixer khi phát

Bạn có thể truyền `AudioMixParameter` để chỉnh exposed parameter trên mixer:

```csharp
SoundManager.Instance.Play(
    "Explosion",
    new AudioMixParameter(AudioMixerExposedParameter.SFX_LOWPASS_CUTOFF_FREQUENCY, 1200f));
```

Ví dụ khác:

```csharp
MusicManager.Instance.Play(
    "MainTheme",
    new AudioMixParameter(AudioMixerExposedParameter.MUSIC_VOLUME, -10f));
```

Tên parameter nên lấy từ file generated `AudioMixerExposedParameter` thay vì tự gõ tay.

## 8. Dùng AudioPlayer trong Inspector

Component `AudioPlayer` cho phép chọn audio trực tiếp trong Inspector.

Các field chính:

- `audioType`: chọn `SFX` hoặc `MFX`.
- `categoryName`: dropdown category theo `audioType`.
- `sfxName` hoặc `mfxName`: dropdown audio name theo category.
- `mixerGroups`: danh sách `AudioMixParameter` áp vào lúc phát.
- `fadeOnChange`: stop audio cũ bằng fade ngắn.

API sẵn có trên component này:

- `Play()`
- `PlayOneShot()`
- `PlayAt(Vector3 position)`
- `PlayFollow(Transform follow)`
- `Pause()`
- `Stop()`

Đây là lựa chọn phù hợp cho:

- button UI
- trigger đơn giản
- object trong scene muốn tự phát audio

## 9. Volume và stop

Mỗi manager có property `Volume` riêng:

```csharp
SoundManager.Instance.Volume = 0.8f;
MusicManager.Instance.Volume = 0.5f;
```

Giá trị này được lưu bằng `PlayerPrefs` theo key manager.

Stop toàn bộ:

```csharp
SoundManager.Instance.StopAll();
MusicManager.Instance.StopAll();
```

Stop toàn bộ có fade:

```csharp
MusicManager.Instance.StopAll(0.25f);
```

Stop theo `AudioSource` trả về:

```csharp
var source = SoundManager.Instance.Play("Explosion");
SoundManager.Instance.Stop(source, 0.15f);
```

## 10. Quy trình làm việc khuyến nghị

Khi thêm audio mới, flow nên là:

1. Tạo `AudioContentSO`.
2. Gán vào `AudioCategorySO`.
3. Đảm bảo category đã nằm trong album đúng (`SFX` hoặc `MFX`).
4. Đảm bảo album đã được add vào `AudioDataManagerSO`.
5. Chọn `AudioDataManagerSO` và bấm `Build AudioData`.
6. Nếu có thay đổi mixer/group, chạy lại menu `Tools/Audio` tương ứng.

## 11. Lỗi thường gặp

### Gọi `Play` nhưng không ra tiếng

Kiểm tra lần lượt:

- `SoundManager` hoặc `MusicManager` có tồn tại trong scene không.
- Field `audioManager` trên manager đã được assign chưa.
- `AudioDataManagerSO` có chứa album đúng chưa.
- Audio name gọi trong code có đúng không.
- Album có đang gán `MixerGroup` hợp lệ không.
- Volume mixer hoặc manager có đang bị kéo quá nhỏ không.

### Dropdown category/audio không hiện

Thường là do chưa build lại dữ liệu hoặc `AudioDataManagerSO` đầu tiên chưa chứa dữ liệu hợp lệ.

Hãy kiểm tra:

- asset `AudioDataManagerSO` đã có `SFX` và `MFX`
- category và audio content đã được gán đủ
- đã bấm `Build AudioData`

### Đổi tên asset nhưng code generated chưa cập nhật

Chọn lại `AudioDataManagerSO` và bấm:

```text
Build AudioData
```

## 12. Ví dụ tối thiểu

```csharp
using UnityEngine;
using BattleTurn.AudioManager.Runtime.Implemented;

public sealed class DemoPlayAudio : MonoBehaviour
{
    public void PlayClick()
    {
        SoundManager.Instance.Play("Click");
    }

    public void PlayBgm()
    {
        MusicManager.Instance.Play("MainTheme");
    }
}
```

## 13. Khuyến nghị đặt tên

Để generated code dễ đọc, nên dùng convention đơn giản:

- Album: `SFX`, `MFX`
- Category: `UI`, `Weapon`, `BGM`, `Ambient`
- Audio name: `Click`, `Explosion`, `MainTheme`, `RainLoop`

Không nên dùng:

- tên có khoảng trắng
- tên thay đổi liên tục
- tên trùng nhau giữa nhiều audio trong cùng category

---

Nếu cần, tôi có thể viết tiếp một bản README thứ hai theo kiểu ngắn gọn hơn cho người dùng cuối trong team, hoặc bổ sung luôn phần ví dụ setup hoàn chỉnh cho scene đầu tiên của game.