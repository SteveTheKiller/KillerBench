# Mã hóa ổ đĩa bằng BitLocker

Dùng hướng dẫn này để bật BitLocker, sao lưu khóa khôi phục và xác nhận ổ đĩa đã được bảo vệ. KillerBench sao lưu và xác minh mật khẩu khôi phục trước khi bắt đầu mã hóa, và không bao giờ hiển thị hay ghi nhật ký mật khẩu khôi phục.

## Trước khi bắt đầu

- Chạy KillerBench với quyền quản trị viên.
- Ổ đĩa Windows cần một trình bảo vệ TPM và một trình bảo vệ mật khẩu khôi phục. Ổ dữ liệu chỉ cần mật khẩu khôi phục.
- Quyết định nơi lưu bản sao lưu khóa khôi phục: một tệp nằm ngoài ổ đĩa bạn đang mã hóa, và tùy chọn thêm Active Directory hoặc Microsoft Entra ID.

## Kiểm tra ổ đĩa

1. Mở **BitLocker** và chọn ổ đĩa trong **Ổ đĩa**.
2. Để thao tác ở **Kiểm tra** và nhấn **Chạy chẩn đoán**. Báo cáo hiển thị trạng thái mã hóa, tình trạng bảo vệ và danh tính các trình bảo vệ.
3. Nếu có TPM nhưng chưa sẵn sàng, hãy chuẩn bị nó từ **Bảo mật** trước. KillerBench không bao giờ xóa TPM.

## Thêm các trình bảo vệ bạn cần

1. Mở **Thao tác BitLocker khác**.
2. Chọn **Thêm trình bảo vệ TPM** hoặc **Thêm mật khẩu khôi phục**, nhấn **Xem trước**, đánh dấu **Thực hiện thay đổi này trên ổ đĩa đã chọn khi tôi nhấn Chạy thao tác**, rồi nhấn **Chạy thao tác**.
3. Sao lưu ngay mật khẩu khôi phục mới. KillerBench không hiển thị nó.

## Sao lưu khóa khôi phục và mã hóa

1. Nhấn **Tìm các trình bảo vệ khôi phục** và chọn trình bảo vệ mật khẩu khôi phục.
2. Nhập đường dẫn đầy đủ của tệp sao lưu nằm ngoài ổ đĩa bạn đang mã hóa.
3. Chọn **Mã hóa**, chọn phương thức mã hóa và đánh dấu **Sao lưu khóa khôi phục đã chọn, rồi bắt đầu mã hóa khi tôi chọn Chạy chẩn đoán**.
4. Nhấn **Chạy chẩn đoán**. KillerBench ghi bản sao lưu, xác minh nó, rồi bắt đầu mã hóa một lần.
5. Quá trình mã hóa tiếp tục ở chế độ nền. Chạy **Kiểm tra** sau đó để xác nhận ổ đĩa báo đã được mã hóa hoàn toàn và đang bật bảo vệ.

## Sao lưu lên Active Directory hoặc Entra ID

Trong **Thao tác BitLocker khác**, chọn sao lưu lên thư mục cho trình bảo vệ đã chọn. KillerBench báo cáo rằng Windows đã chấp nhận yêu cầu nhưng không thể đọc bản sao trong thư mục, vì vậy hãy xác nhận nó trong Active Directory hoặc trung tâm quản trị Entra.

## Ổ dữ liệu và tự động mở khóa

Sau khi ổ dữ liệu được mã hóa, **Bật tự động mở khóa** giúp ổ tự mở khóa khi Windows khởi động. Ổ đĩa Windows phải đã được mã hóa hoàn toàn và đang bật bảo vệ.

## Từ dòng lệnh

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Xem [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) và [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Mỗi lệnh có một lệnh `-recover` tương ứng cho yêu cầu bị gián đoạn.

## Liên quan

- [Mục BitLocker](bitlocker.html)
- [Mục Bảo mật](security.html)
- [Cách giữ an toàn khi thay đổi](safety.html)
