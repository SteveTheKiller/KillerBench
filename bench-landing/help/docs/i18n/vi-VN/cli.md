# Dòng lệnh

`killerbench-cli` chạy cùng Engine với ứng dụng máy tính từ PowerShell hoặc bảng điều khiển từ xa. Mọi lệnh có thể thay đổi máy tính đều xem trước theo mặc định và cần một cờ tường minh như `--execute` trước khi thực hiện. Các lệnh in bằng chứng JSON ra đầu ra chuẩn và tiến trình ra luồng lỗi chuẩn.

Hướng dẫn từng bước: [Chạy KillerBench từ script](use-the-cli.html).

Mã thoát 0 nghĩa là bản xem trước hoặc kết quả đã xác minh thành công, 1 là lỗi khởi động, đầu vào hoặc nhật ký, 2 là không nhận dạng được đối số, 3 là bị chặn, thất bại hoặc chưa xác minh, 4 là quy trình cần khởi động lại trước khi có thể xác minh, và 130 là bạn đã dừng nó.

Chạy `killerbench-cli` không kèm đối số để in danh sách này trên máy của bạn.

## Cập nhật và bảo trì

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Xem lại chính xác lượt truyền hoặc quan sát hai ảnh chụp; không thử lại.
- `bits-jobs [--all-users]`: Trạng thái, chủ sở hữu và số byte của tác vụ BITS; không thay đổi lượt truyền.
- `bits-retry JOB_GUID [--execute]`: Xem trước theo mặc định; thử lại tường minh đúng một tác vụ, không khẳng định ứng dụng đã hoàn tất.
- `bits-retry-recover JOB_GUID`: Hai ảnh chụp chỉ đọc; lưu trữ kết quả ổn định mà không thử lại.
- `component-repair-source`: Chính sách nguồn sửa chữa DISM được quản lý và xung đột; không thay đổi.
- `delivery-optimization`: Trạng thái dịch vụ Delivery Optimization chỉ đọc; bằng chứng JSON.
- `delivery-optimization-activity`: Số lượng tác vụ và tổng số byte hiện tại có giới hạn về quyền riêng tư; không có danh tính lượt truyền và không thay đổi.
- `delivery-optimization-policy`: Cài đặt cổ điển và Policy CSP chỉ đọc; loại trừ giá trị nhạy cảm và danh tính nhà cung cấp.
- `feature-service-events`: Đọc lịch sử lỗi chính xác của các dịch vụ Time, Search, WMI, Event Log và âm thanh.
- `feature-update-policy`: Chính sách bản phát hành đích, trì hoãn, tạm dừng và biện pháp bảo vệ chỉ đọc; bằng chứng JSON.
- `pending-file-operations`: Tóm tắt thao tác tệp đang chờ chỉ đọc, có giới hạn về quyền riêng tư; bằng chứng JSON.
- `restart-check`: Nguồn khởi động lại đang chờ chỉ đọc; bằng chứng JSON.
- `servicing-logs`: Phân tích có giới hạn nhật ký CBS mới nhất, kho lưu trữ CBS dạng văn bản và nén, nhật ký DISM và Windows Setup; không sửa chữa.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Ảnh chụp hoạt động bảo trì chỉ đọc.
- `servicing-service-events`: Lịch sử lỗi chính xác của chuỗi bảo trì Windows Update chỉ đọc.
- `servicing-service-states`: Trạng thái hiện tại của sáu dịch vụ trong chuỗi bảo trì Windows Update chỉ đọc.
- `setup-code DECIMAL_OR_0xHEX`: Tham chiếu mã kết quả Windows Setup đã được ghi trong tài liệu; không kiểm tra máy và không sửa chữa.
- `update-applicability`: Bằng chứng về phiên bản, bản dựng và kiến trúc Windows chỉ đọc.
- `update-cache`: Tổng dung lượng bộ nhớ đệm SoftwareDistribution có giới hạn, chỉ đọc; không có tên tệp.
- `update-code DECIMAL_OR_0xHEX`: Tham chiếu mã kết quả Windows Update đã được ghi trong tài liệu; không kiểm tra máy và không sửa chữa.
- `update-context`: Bằng chứng về nâng quyền và phiên màn hình nền hiện tại chỉ đọc; không có mã định danh tài khoản.
- `update-engine-files`: Bằng chứng về sự hiện diện, phiên bản và chữ ký của bộ máy Windows Update gốc chỉ đọc.
- `update-events`: Lịch sử Windows Update Client trong bảy ngày gần nhất chỉ đọc; JSON kèm bằng chứng thô.
- `update-followup DECIMAL_OR_0xHEX`: Chẩn đoán mới chỉ đọc được chọn theo một mã kết quả Windows Update đã được ghi trong tài liệu.
- `update-history`: 100 kết quả thao tác Windows Update mới nhất chỉ đọc; bằng chứng JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Lặp lại `--select`; xem trước theo mặc định.
- `update-policy`: Chính sách Windows Update được quản lý chỉ đọc; bằng chứng JSON.
- `update-policy-surfaces`: Bằng chứng về bề mặt cấu hình cổ điển và Policy CSP chỉ đọc; loại trừ giá trị và danh tính nhà cung cấp.
- `update-recover`: Kiểm tra một bản cập nhật đang chờ; không bao giờ cài đặt hay thử lại.
- `update-restart-policy`: Hạn tuân thủ, thời gian ân hạn, chặn khởi động lại và hành vi thông báo chỉ đọc.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Kết quả tìm kiếm và kết quả khớp dạng JSON; không cài đặt.
- `update-service-events`: Lịch sử lỗi chính xác của dịch vụ Windows Update chỉ đọc.
- `update-service-files`: Bằng chứng về đăng ký và chữ ký DLL chính xác của dịch vụ Windows Update chỉ đọc.
- `update-services`: Đăng ký dịch vụ Windows Update chỉ đọc; bằng chứng JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Cần tìm kiếm mới; xem trước theo mặc định; giữ lại kho cũ.
- `update-store-reset-recover`: Đối chiếu một lần đặt lại kho đã chuẩn bị mà không lặp lại.
- `windows-capabilities`: Trạng thái khả năng của Windows trực tuyến chỉ đọc; kết quả JSON.
- `windows-update-task`: Trạng thái chính xác và lịch sử chạy của tác vụ Scheduled Start chỉ đọc; không chạy tác vụ và không thao tác cập nhật.

## BitLocker và TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Xem trước theo mặc định; bật tự động mở khóa cho một ổ dữ liệu đã mã hóa.
- `bitlocker-auto-unlock-recover`: Xác minh một yêu cầu tự động mở khóa bị gián đoạn mà không lặp lại.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Xem trước theo mặc định; chỉ tạo tệp sao lưu khôi phục mới.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Xác minh một bản sao lưu bị gián đoạn mà không ghi lại lần nữa.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Xem trước theo mặc định; bắt đầu giải mã, ví dụ trước khi mã hóa lại bằng phương thức bắt buộc.
- `bitlocker-decrypt-recover`: Xác minh một yêu cầu giải mã bị gián đoạn mà không lặp lại.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Xem trước theo mặc định; cần các trình bảo vệ TPM và khôi phục hiện có.
- `bitlocker-encrypt-recover`: Xác minh trạng thái chuyển đổi đang chờ mà không lặp lại yêu cầu.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Xem trước theo mặc định; sao lưu mật khẩu khôi phục hiện có lên Active Directory hoặc Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Chỉ xem trước tùy chọn.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Xem trước theo mặc định; thêm một trình bảo vệ TPM hoặc mật khẩu khôi phục; mật khẩu không bao giờ được in ra.
- `bitlocker-protector-recover`: Xác minh một yêu cầu trình bảo vệ bị gián đoạn mà không lặp lại.
- `bitlocker-protectors DRIVE:`: Chỉ danh tính và loại trình bảo vệ; không có mật khẩu khôi phục.
- `bitlocker-resume DRIVE: [--execute]`: Xem trước theo mặc định; chỉ đúng ổ đĩa đã mã hóa hoàn toàn và đang tạm ngưng bảo vệ.
- `bitlocker-resume-recover`: Xác minh trạng thái bảo vệ đang chờ mà không lặp lại yêu cầu.

## Bảo mật và quyền riêng tư

- `certificate-chains`: Chuỗi tin cậy cục bộ chỉ dùng bộ nhớ đệm cho chứng chỉ cá nhân, chỉ đọc; không kiểm tra thu hồi và không tải xuống.
- `defender-overview [--report]`: Tổng quan về bảo vệ, thời điểm quét và mối đe dọa tổng hợp chỉ đọc; không thao tác.
- `defender-protection [--report]`: Tùy chọn bảo vệ và bề mặt chính sách được nhận dạng chỉ đọc; loại trừ chi tiết nhạy cảm; không thay đổi.
- `defender-scan Quick|Full [--execute] [--report]`: Xem trước theo mặc định; một lần quét Microsoft Defender tường minh, được bảo vệ.
- `defender-scan-history [--report]`: Thời điểm quét nhanh và quét toàn bộ cùng trạng thái quá hạn chỉ đọc; loại trừ chi tiết nhạy cảm; không quét.
- `defender-scan-recover [--report]`: Đối chiếu chỉ đọc mà không bắt đầu lần quét khác.
- `defender-threats [--report]`: Lịch sử mối đe dọa tổng hợp, cờ đang hoạt động hiện tại và ID mức độ nghiêm trọng; loại trừ chi tiết nhạy cảm; không thao tác.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Xem trước theo mặc định; chỉ hồ sơ cục bộ không được quản lý.
- `firewall-enable-recover`: Kiểm tra một thay đổi đang chờ mà không lặp lại.
- `firewall-events`: Đọc lịch sử hoạt động có giới hạn gồm cảnh báo, lỗi và sự kiện nghiêm trọng của Windows Firewall.
- `firewall-logging`: Đọc cấu hình ghi nhật ký lưu lượng cho hồ sơ Miền, Riêng tư và Công cộng mà không mở nhật ký.
- `firewall-rules`: Đọc số lượng quy tắc hiệu lực và nguồn chính sách có giới hạn về quyền riêng tư.
- `firewall-service-events`: Đọc lịch sử lỗi chính xác của Service Control Manager cho BFE và Windows Defender Firewall.
- `privacy-clipboard-history on|off [--execute]`: Xem trước theo mặc định; đúng giá trị của người dùng hiện tại, giữ nguyên chính sách.
- `privacy-clipboard-history-recover`: Kiểm tra lại một thay đổi bị gián đoạn mà không lặp lại.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Khôi phục có bảo vệ giá trị trước đó.
- `privacy-plan --select Camera --select ClipboardSync`: Chỉ xem lại lựa chọn; không thay đổi cài đặt nào.
- `privacy-setting KEY on|off [--execute]`: Xem trước theo mặc định; thay đổi cho người dùng hiện tại, giữ nguyên chính sách; khóa không hợp lệ sẽ liệt kê các lựa chọn được hỗ trợ.
- `privacy-setting-recover KEY`: Kiểm tra lại một thay đổi bị gián đoạn mà không lặp lại.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Khôi phục có bảo vệ đúng giá trị.
- `security-check`: TPM, Secure Boot, các biện pháp bảo vệ ảo hóa và chính sách UAC chỉ đọc; bằng chứng JSON.

## Dịch vụ

- `service-binaries`: Sự hiện diện tệp nhị phân dịch vụ cục bộ và trạng thái Authenticode chỉ đọc; loại trừ đường dẫn và không liên hệ đường dẫn không cục bộ.
- `service-check NAME [NAME ...] | service-check --all`: Trạng thái dịch vụ được hỗ trợ và bằng chứng phụ thuộc chỉ đọc.
- `service-dependencies`: Xem lại tính toàn vẹn của phụ thuộc trực tiếp chỉ đọc; không thay đổi dịch vụ.
- `service-dlls`: Sự hiện diện DLL dịch vụ đã đăng ký và trạng thái Authenticode chỉ đọc; loại trừ đường dẫn và không liên hệ đường dẫn không cục bộ.
- `service-events`: Lịch sử lỗi dịch vụ chỉ đọc; JSON kèm bằng chứng thô.
- `time-events`: Lịch sử sự kiện Windows Time gần đây chỉ đọc kèm bằng chứng JSON.
- `service-logons`: Xem lại loại tài khoản dịch vụ và tương tác màn hình nền chỉ đọc; loại trừ danh tính tài khoản.
- `service-path-quoting`: Xem lại dấu ngoặc kép trong đường dẫn tệp thực thi của dịch vụ chỉ đọc; loại trừ đường dẫn đầy đủ và đối số.
- `service-recovery-policy`: Tóm tắt hành động khôi phục của dịch vụ đã cài chỉ đọc; loại trừ lệnh và thông báo khởi động lại.
- `service-startup-config`: Xem lại tính nhất quán của chế độ khởi động chỉ đọc; không thay đổi dịch vụ.
- `service-transitions`: Quan sát lặp lại trạng thái dịch vụ đang chờ chỉ đọc; không thao tác dịch vụ.
- `service-triggers`: Tóm tắt trình kích hoạt của dịch vụ đã cài chỉ đọc; loại trừ nội dung trình kích hoạt; không thay đổi.

## Lưu trữ, tệp và sao lưu

- `backup-events`: Lịch sử VSS và volsnap; bằng chứng JSON; không thay đổi.
- `backup-service-events`: Đọc lịch sử lỗi chính xác của VSS, nhà cung cấp bản sao bóng phần mềm, Windows Backup và bộ máy sao lưu cấp khối.
- `backup-summary`: Tóm tắt thao tác Windows Backup cục bộ; không thay đổi sao lưu.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Xem trước theo mặc định; Thùng rác được xóa toàn bộ hoặc không xóa gì, tuổi không áp dụng.
- `disk-cleanup-task`: Trạng thái chính xác và lịch sử chạy của tác vụ chỉ đọc; không chạy tác vụ và không dọn dẹp.
- `disk-diagnostic`: Đối chiếu đúng các tác vụ chẩn đoán đĩa và lịch sử sự kiện; không thao tác tác vụ hay đĩa.
- `disk-diagnostic-tasks`: Lịch sử chính xác của tác vụ thu thập và phân giải chỉ đọc; không thao tác tác vụ hay đĩa.
- `disk-quotas`: Giá trị hạn ngạch mặc định và trạng thái của từng ổ đĩa chỉ đọc; loại trừ danh tính người dùng; không thay đổi.
- `file-history-events`: Số cảnh báo và lỗi của Lịch sử tệp đã chiếu theo quyền riêng tư; không thay đổi.
- `file-history-status`: Trạng thái bảo vệ Lịch sử tệp của tài khoản hiện tại; không thay đổi.
- `search-index-rebuild [--execute]`: Xem trước theo mặc định; giữ lại chỉ mục cũ và xác minh trạng thái mới.
- `search-index-rebuild-recover`: Đối chiếu chỉ đọc mà không lặp lại lệnh sửa chữa.
- `storage-check DRIVE_LETTER`: Kiểm tra NTFS cố định chỉ đọc; kết quả JSON và tiến trình trực tiếp.
- `storage-events`: Lịch sử lưu trữ chỉ đọc; JSON kèm bằng chứng thô.
- `storage-jobs`: Trạng thái và tiến trình tổng hợp của tác vụ mô-đun Storage chỉ đọc; JSON; không thay đổi.
- `storage-reliability`: Bộ đếm nhiệt độ, hao mòn và I/O chỉ đọc; JSON; không sửa chữa.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Xem trước theo mặc định; thực thi cần xác nhận sao lưu; ổ hệ thống được lên lịch khi khởi động lại.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Chỉ xem trước; không sửa chữa và không lên lịch.
- `storage-repair-recover`: Xác minh một sửa chữa hệ thống tệp đã hoàn tất hoặc đã lên lịch mà không lặp lại.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Thu thập và gắn kết bằng chứng hiện tại; không sửa chữa và không lên lịch.
- `storage-sense-policy`: Chính sách dọn dẹp máy được nhận dạng chỉ đọc; loại trừ cài đặt người dùng và danh tính tệp.
- `storage-sense-task`: Trạng thái chính xác và lịch sử chạy của tác vụ chỉ đọc; không chạy tác vụ và không dọn dẹp.
- `storage-service-events`: Đọc lịch sử lỗi chính xác của Storage Service, Optimize Drives, VSS và nhà cung cấp bản sao bóng phần mềm.
- `volume-capacity DRIVE:`: Dung lượng khả dụng chỉ đọc kèm bằng chứng JSON.
- `volume-dirty DRIVE:`: Trạng thái bit bẩn chỉ đọc; không sửa chữa và không khởi động lại.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Phân tích chỉ đọc theo mặc định; cần đúng ổ đĩa khỏe mạnh và phương tiện tương thích.
- `volume-optimize-recover`: Xác minh một thao tác đang chờ đã hoàn tất mà không lặp lại.
- `vss-providers`: Tên, phiên bản và loại nhà cung cấp VSS chỉ đọc; loại trừ mã định danh; không thay đổi đăng ký.
- `vss-writers`: Trạng thái trình ghi được báo cáo; diễn giải đầu ra tiếng Anh; không đặt lại.

## In ấn

- `print-events`: Lịch sử PrintService Admin trong bảy ngày gần nhất chỉ đọc; bằng chứng JSON.
- `print-extension-files`: Tóm tắt sự hiện diện và chữ ký tệp nhị phân của bộ xử lý in và trình giám sát in chỉ đọc; loại trừ đường dẫn đầy đủ.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Xem trước theo mặc định; chỉ đúng tác vụ ở trạng thái Error hoặc Blocked.
- `print-job-cancel-recover [--folder PATH]`: Kiểm tra lại một lần hủy bị gián đoạn mà không lặp lại.
- `print-job-list`: Liệt kê các tác vụ hiển thị hiện tại mà không thay đổi chúng.
- `print-operational-events`: Trạng thái kênh PrintService Operational và lịch sử khả dụng chỉ đọc; bằng chứng JSON.
- `print-policy`: Hạn chế Point and Print và chính sách cài trình điều khiển chỉ đọc; bằng chứng JSON.
- `print-rendering-policy`: Chính sách hiển thị và cách ly trình điều khiển chỉ đọc; bằng chứng JSON.
- `print-spooler-events`: Lỗi Service Control Manager gắn với danh tính Print Spooler cục bộ chỉ đọc; bằng chứng JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Cài đặt hàng đợi cục bộ đã chọn chỉ đọc.
- `printer-conditions`: Mã tình trạng hàng đợi cục bộ chỉ đọc; không xác minh vật lý và không thay đổi.
- `printer-driver-files`: Tóm tắt sự hiện diện tệp đã đăng ký và chữ ký tệp thực thi chỉ đọc; loại trừ đường dẫn đầy đủ.
- `printer-driver-models`: Tóm tắt phiên bản 3, phiên bản 4 và nhận biết gói chỉ đọc; bằng chứng JSON.
- `printer-drivers`: Kiểm kê và đối chiếu đăng ký hàng đợi và trình điều khiển chỉ đọc; bằng chứng JSON.
- `printer-endpoint-check`: Kết nối có giới hạn tới đúng các điểm cuối máy in Standard TCP/IP được gán; không khám phá, không SNMP và không dữ liệu in.
- `printer-list`: Tìm hàng đợi cục bộ mà không thay đổi cài đặt.
- `printer-ports`: Đối chiếu đăng ký hàng đợi và cổng chỉ đọc; bằng chứng JSON.
- `printer-queue-attributes`: Thuộc tính spooling, lưu giữ và chia sẻ của hàng đợi cục bộ chỉ đọc.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Xem trước theo mặc định; tắt chế độ ngoại tuyến có thể giải phóng các tác vụ đang chờ.

## Mạng

- `dhcp-check`: Điều kiện của lượt thuê DHCP bị thiếu hoặc hết hạn chỉ đọc; bằng chứng JSON, không gia hạn.
- `dhcp-events`: Lịch sử sự kiện DHCP Client cho IPv4 và IPv6 chỉ đọc; kết quả JSON.
- `dhcp-leases`: Dấu thời gian, thời hạn thuê và bằng chứng máy chủ chỉ đọc; kết quả JSON.
- `dhcp-renew [--execute]`: Xem trước theo mặc định; chỉ đúng các lượt thuê DHCP bị thiếu hoặc hết hạn của bộ điều hợp vật lý đã kết nối.
- `dhcp-renew-recover`: Đối chiếu chỉ đọc mà không yêu cầu lượt thuê khác.
- `dns-cache HOST [--execute]`: Xem trước theo mặc định; xóa bộ nhớ đệm cục bộ có bảo vệ kèm xác minh đúng máy chủ.
- `dns-cache-recover`: Kiểm tra lại một thao tác bộ nhớ đệm bị gián đoạn mà không lặp lại việc xóa.
- `dns-cache-summary`: Trạng thái bộ nhớ đệm DNS cục bộ và số lượng theo loại bản ghi có giới hạn về quyền riêng tư; không thay đổi.
- `dns-check HOST [--timeout-seconds 1..60]`: Phân giải tên hệ thống có giới hạn; kết quả JSON.
- `dns-client-config`: Bằng chứng về hậu tố, đăng ký, rút gọn tên miền và chính sách chỉ đọc; kết quả JSON.
- `dns-client-events`: Lịch sử cảnh báo và lỗi của DNS Client có giới hạn về quyền riêng tư, chỉ đọc; kết quả JSON.
- `dns-nrpt-policy`: Bằng chứng chính sách không gian tên DNS hiệu lực chỉ đọc; kết quả JSON.
- `ip-addresses`: Xem lại trạng thái, nguồn gốc và xung đột địa chỉ IP hiện tại chỉ đọc; bằng chứng JSON.
- `ip-interface-config`: Bằng chứng cấu hình giao diện IP chỉ đọc; kết quả JSON.
- `network-firewall`: Đối chiếu việc gán mạng đang hoạt động với trạng thái bật của hồ sơ tường lửa tương ứng.
- `network-offload`: Cấu hình giảm tải mạng toàn cục của Windows chỉ đọc; kết quả JSON.
- `network-profiles`: Danh mục hồ sơ và phân loại kết nối hiện tại chỉ đọc; loại trừ tên mạng.
- `network-service-events`: Đọc lịch sử lỗi chính xác của DHCP, DNS, Workstation, NLA, Network List và IP Helper.
- `proxy-check`: Bằng chứng proxy WinHTTP đã lưu và proxy Internet của tài khoản hiện tại chỉ đọc.
- `route-priority`: So sánh số liệu tuyến mặc định hiệu lực chỉ đọc; bằng chứng JSON.
- `smb-client-config`: Cấu hình ký SMB, truy cập khách và truyền tải chỉ đọc.
- `smb-connections`: Thương lượng giao thức đang hoạt động chỉ đọc; loại trừ danh tính từ xa.
- `smb-server-config`: Cấu hình giao thức, ký, mã hóa và đa kênh SMB chỉ đọc.
- `smb-shares`: Kiểm kê chia sẻ SMB có giới hạn, chỉ đọc; loại trừ đường dẫn, quyền, phiên và tài khoản.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Kiểm tra kết nối có giới hạn; không gửi dữ liệu ứng dụng.
- `tcp-port-pressure`: Dải TCP động và mức sử dụng cổng cục bộ tổng hợp chỉ đọc; loại trừ điểm cuối và tiến trình.
- `tcp-settings`: Bằng chứng cấu hình mẫu TCP của Windows chỉ đọc; kết quả JSON.
- `vpn-profiles`: Cấu hình hồ sơ VPN của tài khoản hiện tại chỉ đọc; loại trừ thông tin xác thực.
- `winrm-listeners`: Tóm tắt đăng ký trình nghe WinRM chỉ đọc; loại trừ dấu vân tay chứng chỉ.
- `winsock-catalog`: Cấu trúc danh mục giao thức Winsock gốc chỉ đọc; kết quả JSON.
- `winsock-namespaces`: Cấu trúc nhà cung cấp không gian tên Winsock gốc chỉ đọc; kết quả JSON.
- `winsock-provider-files`: Sự hiện diện và chữ ký tệp của nhà cung cấp giao thức chỉ đọc; kết quả JSON.
- `winsock-readiness`: Mức sẵn sàng của giao thức, nhà cung cấp không gian tên và tệp nhà cung cấp Winsock gốc chỉ đọc; kết quả JSON.

## Ứng dụng

- `app-folder-review FOLDER_NAME [--report]`: Xem lại đúng các thư mục trong gốc cố định; không xóa.
- `app-package-review FULL_NAME [--all-users]`: Đúng gói mục tiêu và các gói phụ thuộc; không gỡ bỏ.
- `app-packages [--all-users]`: Chỉ kiểm kê Appx/MSIX đã đăng ký; không gỡ bỏ.
- `browser-extensions`: Xem lại danh tính tiện ích mở rộng Chrome, Edge, Brave và Firefox; không gỡ bỏ.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Chỉ xem lại; không thay đổi hệ thống.
- `depot-review [application and configuration selections] [--json]`: Chỉ xem lại; không cấp phát.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Kiểm tra Authenticode và chữ ký nhà cung cấp chỉ đọc; không chạy gì.
- `foxit-inventory`: Kiểm kê sản phẩm trên máy chỉ đọc; đầu ra JSON.
- `installed-apps [--current-user]`: Đăng ký gỡ cài đặt và cờ khả năng; không gỡ bỏ.
- `installed-apps-compare BEFORE.json AFTER.json`: Khác biệt đăng ký trong danh sách kiểm kê đã lưu; không thay đổi máy.
- `installed-hotfixes`: Kiểm kê có giới hạn các bản sửa nóng Windows đã cài, chỉ đọc; bằng chứng JSON.
- `installer-activity`: Bằng chứng về tiến trình Installer và dấu hiệu giao dịch chỉ đọc; bằng chứng JSON.
- `installer-check`: Chính sách Installer và mức sẵn sàng của bộ máy chỉ đọc; bằng chứng JSON.
- `installer-engine-files`: Sự hiện diện tệp Installer bắt buộc và tóm tắt Authenticode chỉ đọc; bằng chứng JSON.
- `installer-events`: Lịch sử MSI trong bảy ngày gần nhất chỉ đọc; bằng chứng JSON.
- `installer-logging`: Chính sách ghi nhật ký và gỡ lỗi của Installer chỉ đọc; bằng chứng JSON.
- `installer-pending-state`: Đối chiếu thao tác tệp đang chờ với hoạt động Installer chỉ đọc; bằng chứng JSON.
- `installer-registration-repair [--execute]`: Xem trước theo mặc định; chỉ đúng lỗi đăng ký dịch vụ; không bao giờ hủy đăng ký bộ máy.
- `installer-registration-repair-recover`: Kiểm tra lại một lần đăng ký bị gián đoạn mà không lặp lại lệnh.
- `installer-security-policy`: Chính sách bảo mật và bản vá của Installer chỉ đọc; bằng chứng JSON.
- `installer-service-events`: Lỗi Service Control Manager gắn với danh tính Windows Installer cục bộ chỉ đọc; bằng chứng JSON.
- `installer-service-registration`: Tóm tắt đăng ký dịch vụ Windows Installer chỉ đọc; bằng chứng JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Xem trước mới về điều kiện chính xác; không có nhà cung cấp gỡ bỏ.
- `mace-package-recover`: Đối chiếu mới một bản ghi gỡ bỏ gói ứng dụng bị gián đoạn; không bao giờ lặp lại việc gỡ bỏ.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Chỉ xem lại; không gỡ bỏ.
- `msi-cache`: Khả năng thấy bộ nhớ đệm Windows Installer đã đăng ký chỉ đọc; bằng chứng JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Tham chiếu mã trả về Windows Installer đã được ghi trong tài liệu; không kiểm tra máy và không sửa chữa.
- `msi-product {PRODUCT-CODE}`: Trạng thái đăng ký MSI chính xác chỉ đọc; bằng chứng JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Xem trước theo mặc định; chỉ đúng sản phẩm bị hỏng hoặc cấu hình sai.
- `msi-repair-recover`: Kiểm tra lại một lần sửa đúng sản phẩm bị gián đoạn mà không lặp lại.
- `msi-sources`: Siêu dữ liệu nguồn Windows Installer đã đăng ký có giới hạn về quyền riêng tư, chỉ đọc; bằng chứng JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Xem trước theo mặc định; giữ lại đúng bộ nhớ đệm của tài khoản đang chạy dưới tên khôi phục.
- `orca-cache-reset-recover`: Xác minh việc giữ lại bộ nhớ đệm bị gián đoạn mà không lặp lại việc di chuyển.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Chỉ xem lại; không thay đổi Outlook.
- `startup-apps`: Xem lại đăng ký khởi động và tác vụ khi khởi động hoặc đăng nhập; không vô hiệu hóa.
## Hệ thống

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Tra cứu cục bộ chính xác mã gán Bluetooth SIG; không gửi yêu cầu mạng.
- `crash-config`: Chế độ và đường dẫn kết xuất Windows đã lưu; không thay đổi.
- `crash-dumps`: Siêu dữ liệu tệp kết xuất Windows mặc định; không phân tích nội dung và không thay đổi.
- `crash-events`: Báo cáo lỗi hệ thống gần đây; không chẩn đoán và không thay đổi.
- `crash-review [--knowledge PACK_PATH ...]`: Tối đa 32 gói; xem lại kết hợp sự cố, tắt máy, cấu trúc minidump có giới hạn và tham chiếu bug-check chính xác tùy chọn; không thay đổi.
- `default-printer`: Lựa chọn mặc định và cài đặt quản lý tự động chỉ đọc; bằng chứng JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Xem trước theo mặc định; chỉ máy in mặc định bị thiếu hoặc xung đột.
- `default-printer-set-recover [--folder PATH]`: Kiểm tra lại một lựa chọn bị gián đoạn mà không lặp lại.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Bằng chứng thiết bị chỉ đọc kèm tham chiếu trình điều khiển chính xác tùy chọn từ tối đa 32 gói.
- `device-code DECIMAL_OR_0xHEX`: Tham chiếu mã sự cố Device Manager ngoại tuyến.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Chỉ các ứng viên tham chiếu.
- `event-channel-enable CHANNEL [--execute]`: Xem trước theo mặc định; bật đúng một kênh chẩn đoán không được quản lý và giữ nguyên các cài đặt khác.
- `event-channel-enable-recover`: Đối chiếu chỉ đọc mà không lặp lại lệnh bật.
- `event-channels`: Trạng thái, lưu giữ, kích thước và mức đầy của bốn kênh chẩn đoán chỉ đọc.
- `font-registrations`: Kiểm tra có giới hạn đăng ký và sự hiện diện tệp cho máy và người dùng hiện tại, chỉ đọc.
- `memory-diagnostic-results`: Lịch sử Windows Memory Diagnostic có giới hạn, chỉ đọc; bằng chứng JSON.
- `minidump-analyze PATH`: Siêu dữ liệu cấu trúc minidump có giới hạn; không có trang bộ nhớ, ký hiệu hay thay đổi.
- `name-rename PROPOSED_NAME [--execute]`: Xem trước theo mặc định; chỉ PC nhóm làm việc độc lập; không bao giờ khởi động lại Windows.
- `name-rename-recover`: Xác minh một lần đổi tên đang chờ mà không lặp lại.
- `name-review PROPOSED_NAME`: Xem lại danh tính và tên được đề xuất chỉ đọc.
- `profiles --recover`: Xác minh trạng thái vắng mặt đã ghi và giữ bằng chứng khôi phục; không bao giờ thử lại việc xóa.
- `profiles --recover-backup`: Xác minh và lưu trữ một sản phẩm sao lưu đang chờ; không bao giờ thử lại việc sao chép.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Xem trước trước tiên; sao chép gốc có bảo vệ cần thực thi tường minh và đích NTFS cục bộ chưa tồn tại.
- `profiles --select SID --create-backup-artifact DESTINATION`: So sánh một bản sao lưu đã sao chép hiện có và tạo sản phẩm cố định của nó; không sao chép hay xóa hồ sơ.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Xem lại chỉ đọc; việc xác minh cần một SID được chọn.
- `task-check`: Kiểm kê tác vụ, kết luận đã phân loại và bằng chứng JSON chỉ đọc.
- `task-definition TASK_FOLDER TASK_NAME`: XML tác vụ chính xác chỉ đọc; không khả dụng không có nghĩa là không tồn tại.
- `task-inventory`: Danh tính và trạng thái tác vụ chỉ đọc; đầu ra JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Xem lại khôi phục trong quá khứ; không bao giờ cho phép thử lại.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Xác thực và xem lại bản nháp tác vụ; không đăng ký.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Xem trước theo mặc định; định nghĩa Local System bị tắt, chỉ tạo mới.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Đối chiếu định nghĩa chính xác chỉ đọc; không bao giờ thử lại hay chạy tác vụ.
- `task-xml PLAN.json YYYY-MM-DD`: XML xem lại bị tắt ra stdout; chỉ lịch theo ngày trong múi giờ cục bộ; không đăng ký.
- `time-config`: Chế độ, máy ngang hàng và bằng chứng chính sách Windows Time chỉ đọc.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Xem trước theo mặc định; chế độ tùy chỉnh cần máy ngang hàng.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Chỉ xem trước.
- `time-recover --folder PATH`: Xác minh và lưu trữ một bản ghi đang chờ; không bao giờ thử lại thay đổi.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Chỉ đọc; chế độ tùy chỉnh cần máy ngang hàng.
- `wmi-provider`: Truy vấn nhà cung cấp hệ điều hành cục bộ chỉ đọc; bằng chứng JSON.

## Tri thức, xác thực và Engine

- `knowledge PACK_PATH CONTEXT CODE`: Tra cứu tham chiếu cục bộ; không thực thi sửa chữa.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Kích hoạt cục bộ đã xác minh; không tải xuống.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Xác minh gói cục bộ đang hoạt động; không thay đổi.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Chỉ thay đổi tham chiếu; không cài gói.
- `knowledge-info PACK_PATH`: Siêu dữ liệu gói và số mục theo từng ngữ cảnh; không thay đổi.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Gắn tham chiếu theo ngữ cảnh vào các mã quan sát được.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Chọn gói được giữ lại đã xác minh mới nhất; không tải xuống.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Tối đa 50 kết quả khớp văn bản tham chiếu; không thực thi sửa chữa.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Phân giải đúng một danh tính trên tối đa 32 gói; không bao giờ chọn khi xung đột.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Xác minh mọi cơ sở dữ liệu đã cài được cấu hình; không tải xuống và không kích hoạt.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Tải và kích hoạt nguyên tử các cơ sở dữ liệu ứng viên đã ký từ các điểm cuối HTTPS được cấu hình.
- `open <file.kbench> [--json]`: Mở lại một ảnh chụp đã lưu và đánh giá lại bằng các quy tắc đã cài.
- `scan --output <new-file.kbench> [--json]`: Ảnh chụp Windows Update chỉ đọc được lưu vào tệp mới.
- `validation-coverage RECORD.json [...]`: Tóm tắt bằng chứng VM gốc của phiên bản hiện tại; không chẩn đoán và không sửa chữa.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Kế hoạch kịch bản VM dùng một lần chỉ đọc; không chẩn đoán và không sửa chữa.
- `validation-record RECORD.json`: Xác thực một bản ghi bằng chứng VM gốc có giới hạn; không chẩn đoán và không sửa chữa.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Báo cáo phạm vi VM cục bộ của phiên bản hiện tại.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Chạy một kế hoạch tùy chọn dùng chung chỉ đọc đã được xác thực.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Chỉ kiểm kê.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Nhật ký chẩn đoán chuyên biệt; không sửa chữa.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Chỉ xem lại có ghi nhật ký.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Trạng thái sản phẩm chính xác tùy chọn được đưa vào nhật ký.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Chẩn đoán mục tiêu đã chọn tùy chọn được đưa vào nhật ký.
- `workflow storage [--volume D:] [--json]`: Kiểm tra hệ thống tệp chỉ đọc có ghi nhật ký.
- `workflow-journal FILE.json`: Nội dung lịch sử; mã thoát 0 nghĩa là đọc thành công, không phải khỏe mạnh.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Hiển thị hoặc xác thực các điều khiển trung lập với máy chủ; không quét và không sửa chữa.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Chỉ báo cáo lịch sử; không chạy lệnh nào.

## Lệnh khác

- `<analyze|investigate> <text-log-path> [--json]`: Phân tích bằng chứng văn bản cục bộ.
- `adapter-bindings`: Tóm tắt liên kết giao thức lõi chỉ đọc; bằng chứng JSON.
- `adapter-statistics`: Bộ đếm byte, lỗi và gói bị loại của bộ điều hợp chỉ đọc; bằng chứng JSON.
- `application-events`: Lỗi Application gần đây, số lượng theo nhóm và bằng chứng JSON chỉ đọc.
- `applocker-policy`: Chế độ thu thập hiệu lực và số lượng quy tắc tổng hợp chỉ đọc; loại trừ chi tiết quy tắc; không thay đổi.
- `common-check [--scope NAME ...]`: Mọi vùng chẩn đoán theo mặc định; chỉ các vùng đã chọn khi được chỉ định; JSON chỉ đọc.
- `delete-notifications`: Cấu hình thông báo xóa của NTFS và ReFS chỉ đọc; JSON; không tối ưu hóa và không thay đổi cài đặt.
- `diagnostic-scopes [--all]`: Liệt kê 11 nhóm kết quả được đề xuất, hoặc tất cả 49 vùng chỉ đọc; không thu thập.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Nhật ký chẩn đoán chuyên biệt; không sửa chữa.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Mọi vùng chẩn đoán; không sửa chữa trừ khi chỉ định tường minh `--auto-repair`.
- `gateway-check`: Kiểm tra ICMP có giới hạn tới đúng các bước nhảy kế tiếp mặc định đã cấu hình; không quét địa chỉ.
- `infrastructure-service-events`: Đọc một ảnh chụp lỗi chính xác, có giới hạn của các dịch vụ kết nối, quản lý từ xa, thông báo và dịch vụ lõi.
- `investigate-batch FILE [FILE ...]`: Tối đa 12 tệp; JSON Lines; mã thoát 3 cho lần nhập thất bại hoặc một phần.
- `licensing-check`: Trạng thái kích hoạt Windows không có khóa sản phẩm, chỉ đọc; bằng chứng JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Tra cứu gói cục bộ theo tiền tố dài nhất; không gửi yêu cầu mạng.
- `ncsi-policy`: Chính sách phân loại kết nối Windows chỉ đọc; loại trừ đích thăm dò.
- `neighbor-cache`: Bằng chứng khả năng truy cập máy lân cận có giới hạn về quyền riêng tư, chỉ đọc; kết quả JSON.
- `ntfs-self-healing DRIVE:`: Cấu hình sửa NTFS trực tuyến của ổ đĩa đã chọn chỉ đọc; không sửa chữa và không thay đổi cài đặt.
- `pagefile-check`: Tóm tắt quản lý, phân bổ và sử dụng tệp phân trang chỉ đọc; loại trừ đường dẫn đầy đủ.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Tra cứu gói cục bộ chính xác; không gửi yêu cầu mạng và không chọn trình điều khiển.
- `physical-disks`: Trạng thái tình trạng, hoạt động, phương tiện, bus và sử dụng hiện tại chỉ đọc; JSON; không sửa chữa.
- `platform-service-events`: Đọc một ảnh chụp lỗi chính xác, có giới hạn của các dịch vụ ứng dụng, danh tính, hồ sơ và hỗ trợ thiết bị.
- `previous-windows [--execute --backup-confirmed]`: Xem trước theo mặc định; xóa vĩnh viễn khả năng hoàn nguyên Windows thông qua DISM.
- `previous-windows-recover`: Đối chiếu chỉ đọc mà không lặp lại việc xóa khả năng hoàn nguyên.
- `provisioned-packages`: Các gói được cấp sẵn cho người dùng mới trong Windows cục bộ; không gỡ bỏ.
- `recipes [WORKFLOW]`: Liệt kê các định nghĩa đã cài hoặc phụ thuộc và giới hạn riêng của quy trình; không thực thi.
- `repair <dism|sfc> [--execute]`: Xem trước theo mặc định.
- `reserved-storage`: Trạng thái bộ nhớ dành riêng của Windows trực tuyến chỉ đọc; bằng chứng JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Xem trước theo mặc định; tạo một điểm sau khi kiểm tra mới.
- `restore-point-recover`: Xác minh một yêu cầu đang chờ mà không tạo điểm khác.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Chỉ xem trước.
- `shadow-copies`: Trạng thái và thuộc tính ảnh chụp nhanh VSS hiện tại chỉ đọc; loại trừ danh tính; không thay đổi ảnh chụp nhanh.
- `shadow-storage`: Phân bổ và áp lực của vùng vi sai VSS chỉ đọc; JSON; không đổi kích thước và không thao tác ảnh chụp nhanh.
- `uac-policy`: Trạng thái bật UAC và chính sách lời nhắc nâng quyền chỉ đọc; không có dữ liệu tài khoản hay thông tin xác thực.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Tra cứu chính xác trong sổ đăng ký UEFI cục bộ; không gửi yêu cầu mạng và không chọn trình điều khiển.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Tra cứu gói cục bộ chính xác; không gửi yêu cầu mạng và không chọn trình điều khiển.
- `vitals-check`: Ảnh chụp Windows, phần cứng, tài khoản, tài nguyên, pin và đĩa vật lý chỉ đọc; bằng chứng JSON.
- `win32-code DECIMAL_OR_0xHEX`: Tra cứu ngoại tuyến thông báo của Windows đã cài.
- `workflows`: Liệt kê khóa và tiêu đề của các quy trình đã cài; không quét và không sửa chữa.
