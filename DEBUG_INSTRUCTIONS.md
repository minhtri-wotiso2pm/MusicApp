# ?? DEBUG INSTRUCTIONS - Màn Hình ?en Issue

## ?ã thêm Logging chi ti?t

### ?? Log File Location
T?t c? log s? ???c ghi vào: **`D:\musicapp_debug.log`**

### ?? Các thay ??i ?ã th?c hi?n:

1. **App.xaml.cs**:
   - ? Catch t?t c? Unhandled Exceptions
   - ? Log toàn b? quá trình startup
   - ? Log khi LoginWindow ???c t?o và hi?n th?

2. **MainWindow.xaml.cs**:
   - ? Log t?ng b??c trong Constructor
   - ? Log LoadUserPlaylists() chi ti?t
   - ? Log LoadLibrarySongs() chi ti?t
   - ? Log Window_Loaded event
   - ? MessageBox hi?n th? khi load thành công

3. **MainWindow.xaml**:
   - ? T?m th?i t?t `AllowsTransparency="True"`
   - ? ??i `WindowStyle="None"` ? `WindowStyle="SingleBorderWindow"`
   - ? Thêm `Loaded="Window_Loaded"` event

## ?? Cách Test:

### B??c 1: Xóa log c? (n?u có)
```powershell
Remove-Item D:\musicapp_debug.log -ErrorAction SilentlyContinue
```

### B??c 2: Ch?y App
1. Start application
2. Login v?i account c?a b?n
3. Quan sát:
   - Có MessageBox "MainWindow loaded successfully!" không?
   - Có l?i nào hi?n ra không?
   - Màn hình có ?en không?

### B??c 3: Ki?m tra Log
```powershell
notepad D:\musicapp_debug.log
```

## ?? Phân Tích Log:

### ? N?u th?y dòng này ? App kh?i ??ng OK:
```
[APP] - ====== APPLICATION STARTED ======
[APP] - ===== OnStartup START =====
[APP] - Creating LoginWindow...
[APP] - LoginWindow shown successfully
```

### ? N?u th?y dòng này ? MainWindow constructor OK:
```
[MainWindow] - ===== MainWindow Constructor START =====
[MainWindow] - Step 1: InitializeComponent DONE
[MainWindow] - Step 8: UpdateEmptyState DONE
[MainWindow] - ===== MainWindow Constructor COMPLETED SUCCESSFULLY =====
```

### ? N?u th?y dòng này ? Có Exception:
```
[MainWindow] - !!!!! EXCEPTION in Constructor:
[APP] - !!!!! UNHANDLED DISPATCHER EXCEPTION !!!!!
```

## ?? Các V?n ?? Có Th?:

### 1. Database Connection Timeout
**Tri?u ch?ng**: Log d?ng l?i ? "Getting playlists from service..."
**Gi?i pháp**: 
- Ki?m tra SQL Server có ?ang ch?y không
- Ki?m tra connection string trong appsettings.json

### 2. UI Thread Blocking
**Tri?u ch?ng**: Log hoàn t?t nh?ng màn hình v?n ?en
**Gi?i pháp**:
- Database query quá lâu ? Move sang async
- File I/O blocking ? Optimize

### 3. WPF Rendering Issue
**Tri?u ch?ng**: Log OK, Window_Loaded fired, nh?ng v?n ?en
**Gi?i pháp**:
- Update GPU driver
- Force software rendering (?ã thêm trong App.xaml.cs c?)
- T?t AllowsTransparency (?ã làm)

### 4. Exception Silent Fail
**Tri?u ch?ng**: Không có log sau m?t b??c nào ?ó
**Gi?i pháp**:
- Ki?m tra Inner Exception trong log
- Check MessageBox có hi?n không

## ?? Screenshots ?? g?i:

N?u v?n l?i, hãy g?i:
1. **Toàn b? file log**: `D:\musicapp_debug.log`
2. **Screenshot** khi app ?ang ?en
3. **Task Manager**: CPU/Memory usage khi app ?en
4. **Event Viewer**: Windows Logs ? Application

## ?? Next Steps n?u v?n ?en:

1. Check log file có ???c t?o không
2. N?u không có log ? Exception x?y ra tr??c c? khi log ???c setup
3. N?u có log nh?ng d?ng gi?a ch?ng ? Tìm dòng cu?i cùng ?? bi?t b? fail ? ?âu
4. N?u log hoàn t?t nh?ng v?n ?en ? WPF rendering issue

## ?? Quick Fixes th? ngay:

```csharp
// Trong App.xaml.cs OnStartup, thêm:
System.Windows.Media.RenderOptions.ProcessRenderMode = 
    System.Windows.Interop.RenderMode.SoftwareOnly;
```

ho?c t?t Hardware Acceleration:
```csharp
// Trong MainWindow constructor, sau InitializeComponent():
this.UpdateLayout();
this.InvalidateVisual();
```
