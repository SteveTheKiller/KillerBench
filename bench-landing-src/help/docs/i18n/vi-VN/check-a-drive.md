# Kiểm tra và sửa ổ đĩa

Dùng hướng dẫn này khi một ổ đĩa báo lỗi, Windows yêu cầu quét ổ đĩa hoặc tệp không mở được. KillerBench kiểm tra ổ đĩa ở chế độ chỉ đọc trước, và chỉ lên lịch hoặc chạy sửa chữa hệ thống tệp sau khi bạn xác nhận đã sao lưu.

## Trước khi bắt đầu

- Sao lưu dữ liệu quan trọng trên ổ đĩa. Sửa chữa hệ thống tệp có thể thay đổi hoặc xóa các tệp bị hỏng.
- Chạy KillerBench với quyền quản trị viên.

## Kiểm tra ổ đĩa

1. Mở **Lưu trữ** và chọn **Lưu trữ**.
2. Chọn ổ đĩa trong **Ổ đĩa cần kiểm tra** và nhấn **Chạy chẩn đoán**.
3. Đọc báo cáo. Báo cáo hiển thị tình trạng đĩa và ổ đĩa, bit bẩn, khả năng tự phục hồi trực tuyến của NTFS, lỗi thiết bị bộ điều khiển và đĩa, bộ đếm độ tin cậy và các sự kiện lưu trữ gần đây như đặt lại, thử lại và tháo bất ngờ.

Kết quả kiểm tra sạch nghĩa là không có sửa chữa nào được đề xuất. Nhiệt độ đĩa, độ hao mòn và bộ đếm I/O chưa được sửa là bằng chứng để xem lại và tự chúng không kích hoạt sửa chữa.

## Sửa hệ thống tệp

1. Nhấn **Xem lại việc sửa chữa hệ thống tệp** và chọn **Sửa chữa tại chỗ** hoặc **Quét và sửa chữa ngoại tuyến**.
2. Với ổ dữ liệu, đánh dấu **Cho phép đưa ổ đĩa không phải hệ thống sang ngoại tuyến** nếu bạn có thể đóng mọi thứ đang dùng nó. Ổ đĩa Windows không thể chuyển sang ngoại tuyến, vì vậy sửa chữa của nó được lên lịch cho lần khởi động lại tiếp theo.
3. Đánh dấu **Dữ liệu quan trọng trên ổ đĩa này đã được sao lưu**.
4. Xác nhận sửa chữa. KillerBench kiểm tra lại đúng ổ đĩa đó ngay trước khi thực hiện và ghi một mục chờ trước.
5. Với sửa chữa đã lên lịch, hãy khởi động lại khi thuận tiện. KillerBench xác minh kết quả sau khi khởi động lại mà không chạy lại sửa chữa.

## Tối ưu hóa hoặc retrim

Cùng công cụ đó có thể phân tích ổ đĩa và chạy tối ưu hóa được Windows đề xuất, **Retrim bộ nhớ thể rắn** hoặc chống phân mảnh ổ đĩa cứng. Mỗi thao tác là một hành động được xác nhận riêng trên đúng một ổ đĩa khỏe mạnh.

## Từ dòng lệnh

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Xem [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) và [volume-optimize](cli.html#cmd-volume-optimize).

## Liên quan

- [Mục Lưu trữ](storage.html)
- [Giải phóng dung lượng ổ đĩa](free-disk-space.html)
- [Khắc phục sự cố](troubleshooting.html)
