# Chạy KillerBench từ script

Dùng dòng lệnh khi bạn muốn có bằng chứng của KillerBench trong một script, một tác vụ RMM hoặc một bảng điều khiển từ xa. `killerbench-cli` chạy cùng Engine với ứng dụng máy tính và in ra JSON.

## Bắt đầu

1. Mở PowerShell tại nơi có `killerbench-cli.exe`, hoặc thêm thư mục của nó vào đường dẫn.
2. Chạy `killerbench-cli help` để liệt kê mọi lệnh cùng ranh giới an toàn của chúng.
3. Chạy `killerbench-cli diagnostic-scopes` để xem 11 nhóm chẩn đoán được đề xuất.

## Chạy quét và lưu báo cáo

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` chỉ đọc trừ khi bạn thêm `--auto-repair`. `--report` ghi cùng một báo cáo có chủ đề như ứng dụng xuất ra. Dùng `--scope` để chỉ kiểm tra một số vùng, và `common-check` để có JSON kết hợp mà không có nhật ký quy trình.

## Đọc mã thoát

| Mã | Ý nghĩa |
| --- | --- |
| 0 | Bản xem trước hoặc kết quả đã xác minh thành công. |
| 1 | Lỗi khởi động, đầu vào hoặc nhật ký đã dừng lệnh. |
| 2 | Không nhận dạng được các đối số. |
| 3 | Bị chặn, thất bại hoặc chưa xác minh. |
| 4 | Quy trình cần khởi động lại trước khi có thể xác minh. |
| 130 | Bạn đã dừng nó. |

Mã thoát 0 trên một lệnh chỉ đọc nghĩa là lệnh đã chạy, không phải máy tính khỏe mạnh. Hãy đọc JSON để biết kết luận.

## Thực hiện thay đổi

Mọi lệnh có thể thay đổi máy tính đều xem trước theo mặc định. Đọc bản xem trước, rồi chạy cùng lệnh đó với `--execute`. Nếu một yêu cầu bị gián đoạn, hãy chạy lệnh `-recover` tương ứng; lệnh này báo cáo kết quả thực và không bao giờ lặp lại thay đổi.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Mở lại kết quả cũ

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Dùng từ trợ lý AI

[KillerMCP](../mcp.html) có thể gọi CLI đã cài để tra cứu mã lỗi của Device Manager và Windows.

## Liên quan

- [Tham chiếu dòng lệnh](cli.html)
- [Hướng dẫn dành cho nhà phát triển Engine](../engine/index.html)
- [Khắc phục sự cố](troubleshooting.html)
