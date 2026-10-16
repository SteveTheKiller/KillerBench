# Tăng quyền riêng tư cho Windows

Dùng hướng dẫn này để xem lại và siết chặt quyền riêng tư trên máy tính xách tay. KillerBench đọc các cài đặt hiện tại, cho biết cài đặt nào đã do chính sách hoặc quản lý thiết bị sở hữu, và chỉ thay đổi các cài đặt bạn chọn, từng cái một, với khả năng hoàn tác chính xác.

## Xem lại cài đặt hiện tại

1. Mở **Quyền riêng tư**.
2. Đánh dấu các vùng bạn muốn xem lại. Mọi vùng đều bắt đầu ở trạng thái không đánh dấu.
3. Nhấn **Chạy chẩn đoán**. Báo cáo hiển thị giá trị hiện tại cho máy này và tài khoản đang chạy KillerBench, và đánh dấu các cài đặt do chính sách sở hữu.

## Thay đổi một cài đặt

1. Chọn cài đặt trong **Cài đặt quyền riêng tư**, như máy ảnh, micrô, vị trí, dữ liệu chẩn đoán, quảng cáo, lịch sử bảng tạm, tìm kiếm nội dung đám mây hoặc điểm nổi bật tìm kiếm.
2. Chọn giá trị mới và đánh dấu **Áp dụng khi tôi chọn Chạy chẩn đoán**.
3. Nhấn **Chạy chẩn đoán**. KillerBench kiểm tra chủ sở hữu chính sách, ghi lại giá trị hiện tại, thực hiện đúng một thay đổi và đọc lại.

Các cài đặt do chính sách nhóm hoặc quản lý thiết bị sở hữu được giữ nguyên. Hãy thay đổi chúng tại nơi chúng được quản lý.

## Hoàn tác một thay đổi

Mỗi thay đổi đã xác minh đều giữ một nhật ký. Thao tác hoàn tác tương ứng khôi phục chính xác giá trị trước đó, một lần nữa sau khi kiểm tra chính sách.

## Làm gọn Windows

Với đề xuất, nội dung tiêu dùng, Recall và tính năng AI, các mục thừa trên thanh tác vụ và menu Bắt đầu, Edge và phần mềm cài sẵn, hãy mở **Bảo trì** và chọn **Làm gọn Windows**. Việc xem lại chỉ đọc và không có gì được chọn tự động.

## Từ dòng lệnh

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Xem [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) và [debloat-review](cli.html#cmd-debloat-review). Một khóa không được hỗ trợ sẽ liệt kê các lựa chọn được hỗ trợ.

## Liên quan

- [Mục Quyền riêng tư](privacy.html)
- [Thiết lập máy tính mới](set-up-new-pc.html)
