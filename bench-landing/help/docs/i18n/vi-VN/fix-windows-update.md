# Sửa Windows Update

Dùng hướng dẫn này khi bản cập nhật thất bại, bị treo hoặc cứ cài đặt lại. KillerBench đọc chuỗi cập nhật trước, giải thích lỗi kèm bằng chứng đằng sau, và chỉ khởi động dịch vụ hoặc sửa kho thành phần khi bằng chứng mới chứng minh đúng vấn đề đó.

## Trước khi bắt đầu

- Chạy KillerBench với quyền quản trị viên. Dịch vụ cập nhật, kho thành phần và tệp hệ thống được bảo vệ cần nâng quyền để có bằng chứng đầy đủ.
- Khởi động lại trước nếu Windows đang chờ khởi động lại. Nhiều sửa chữa cập nhật bị chặn cho đến khi trạng thái khởi động lại rõ ràng.
- Giữ máy tính cắm nguồn điện khi thực hiện công việc bảo trì.

## Tìm hiểu vì sao cập nhật thất bại

1. Mở **Cập nhật** và chọn **Windows Update** trong danh sách công cụ.
2. Để **Tự động sửa các vấn đề đã xác nhận** tắt cho lượt đầu và nhấn **Chạy chẩn đoán**.
3. Đọc **Báo cáo dễ đọc**. Báo cáo liệt kê các dịch vụ cập nhật đã dừng, lần khởi động lại đang chờ, dung lượng trống thấp, kho thành phần bị hỏng và các mã lỗi chính xác mà Windows đã ghi, kèm bước tiếp theo được đề xuất.
4. Nếu báo cáo nêu một mã lỗi, các kiểm tra tiếp theo tương ứng sẽ tự động chạy. Bạn cũng có thể tự tra cứu bằng `killerbench-cli update-code 0x80070002`.

## Theo dõi một bản cập nhật có vẻ bị treo

Chọn **Theo dõi hoạt động bảo trì** trong **Cập nhật** và nhấn **Chạy chẩn đoán**. Công cụ chụp nhiều lần hoạt động cập nhật, Setup và Installer, tổng lượng truyền và các chỉ báo khởi động lại. Hoạt động chỉ được hiển thị làm bằng chứng; công cụ không bao giờ tự khẳng định có treo hay có lỗi.

## Sửa những gì báo cáo tìm thấy

1. Chạy lại **Windows Update** với **Tự động sửa các vấn đề đã xác nhận** được bật. Công cụ có thể khởi động các dịch vụ cập nhật đang bật đã được xác nhận là dừng, và chỉ chạy DISM hoặc SFC khi kiểm tra trực tiếp xác nhận có hỏng hóc.
2. Với một dịch vụ riêng lẻ, hãy chọn công cụ chuyên biệt của nó, như **Dịch vụ BITS**, **Dịch vụ mật mã**, **Trình cài đặt mô-đun Windows** hoặc **Dịch vụ Update Orchestrator**. Mỗi công cụ chỉ đề xuất khôi phục cho đúng dịch vụ đó.
3. Khi báo cáo yêu cầu khởi động lại, hãy khởi động lại trước khi chạy bất cứ thứ gì khác. KillerBench ghi lại các kết quả cần khởi động lại và hoãn công việc sau đó cho đến khi khởi động lại xong.

Mọi sửa chữa đều được ghi nhật ký và đọc lại. Xem [Cách giữ an toàn khi thay đổi](safety.html) để biết toàn bộ trình tự.

## Cài đặt một bản cập nhật cụ thể

1. Chọn **Bản cập nhật có sẵn** trong **Cập nhật** và nhấn **Chạy chẩn đoán** để tìm bản cập nhật từ nguồn đã cấu hình.
2. Chọn các bản cập nhật bạn muốn và nhấn **Xem trước các bản đã chọn**. Bản xem trước liệt kê chính xác những gì sẽ được cài và mọi yêu cầu khởi động lại.
3. Đọc bản xem trước, đánh dấu ô xác nhận, rồi nhấn **Cài đặt**. Ô đánh dấu được xóa sau mỗi lần chạy, và KillerBench không bao giờ chọn hay phê duyệt bản cập nhật thay bạn.

## Từ dòng lệnh

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Xem [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) và [update-plan](cli.html#cmd-update-plan). Nếu việc cài đặt bị gián đoạn, hãy chạy [update-recover](cli.html#cmd-update-recover); lệnh này kiểm tra kết quả và không bao giờ thử lại bản cập nhật.

## Liên quan

- [Mục Cập nhật](updates.html)
- [Sửa tệp hệ thống](repair-system-files.html)
- [Khắc phục sự cố](troubleshooting.html)
