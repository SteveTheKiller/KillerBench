# Thiết lập máy tính mới

Dùng hướng dẫn này khi chuẩn bị một PC mới hoặc vừa được đặt lại. KillerBench xem lại máy tính, các ứng dụng bạn định cài và tên bạn muốn đặt cho máy, rồi chỉ đến các công cụ được bảo vệ để thực hiện từng thay đổi.

## Bắt đầu với một mốc cơ sở

1. Chạy **Quét toàn bộ hệ thống** từ **Chẩn đoán** và giữ báo cáo làm hồ sơ về trạng thái ban đầu của máy tính.
2. Cài các bản cập nhật đang chờ. Xem [Sửa Windows Update](fix-windows-update.html) nếu có bản nào thất bại.

## Xem lại ứng dụng và cấu hình

1. Mở **Chuẩn bị máy** và chọn **Chuẩn bị máy**.
2. Chọn các ứng dụng và nhóm cấu hình bạn định thiết lập và nhấn **Chạy chẩn đoán**. Việc xem lại so sánh chúng với những gì đã được cài. KillerBench không tải xuống hay chạy trình cài đặt.
3. Trước khi chạy trình cài đặt của nhà cung cấp, hãy xác minh nó từ dòng lệnh. KillerBench chỉ tin tưởng trình cài đặt khi nó mang chữ ký hợp lệ từ đúng tên tổ chức của nhà cung cấp đó.

## Đặt tên máy tính

1. Chọn **Tên máy tính**, nhập tên và nhấn **Chạy chẩn đoán** để xem lại tên so với danh tính và vai trò miền hiện tại.
2. Trên máy tính thuộc nhóm làm việc, đánh dấu **Đổi tên PC nhóm làm việc này sau khi kiểm tra mới** để đổi tên. Cần khởi động lại để hoàn tất.

## Bảo vệ và dọn gọn máy

- [Mã hóa ổ đĩa bằng BitLocker](encrypt-with-bitlocker.html) và sao lưu khóa khôi phục.
- [Tăng quyền riêng tư cho Windows](make-windows-private.html) và xem lại **Làm gọn Windows** trong **Bảo trì**.
- Tạo điểm khôi phục bằng **Chuẩn bị một điểm khôi phục** khi mọi thứ đã sẵn sàng.

## Từ dòng lệnh

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Xem [workflow](cli.html#cmd-workflow) và [restore-point](cli.html#cmd-restore-point).

## Liên quan

- [Mục Chuẩn bị máy](onboarding.html)
- [Lần chẩn đoán đầu tiên](first-run.html)
