# Cập nhật

Cập nhật bao gồm Windows Update, các dịch vụ đứng sau nó và kho thành phần. Bạn có thể xem những gì đã cài, những gì đang chờ và vì sao một bản cập nhật thất bại, đồng thời xem lại các bản cập nhật có sẵn trước khi chọn cài bất kỳ bản nào.

Hướng dẫn từng bước: [Sửa Windows Update](fix-windows-update.html) và [Sửa tệp hệ thống](repair-system-files.html).

## Công cụ trong mục này

Theo dõi hoạt động bảo trì, xem lại các bản cập nhật có sẵn và sửa chuỗi Windows Update.

### Theo dõi hoạt động bảo trì

Quan sát có giới hạn hoạt động bảo trì của Windows Update và Setup qua một hoặc nhiều lần chụp. Các tùy chọn điều khiển nhịp làm mới và việc có bao gồm sự kiện cập nhật, lịch sử cập nhật, tác vụ đã lên lịch chính xác của Windows Update, trạng thái chính xác của dịch vụ, chỉ báo khởi động lại, truyền nền của tài khoản hiện tại, hoạt động Delivery Optimization và hoạt động Windows Installer có giới hạn về quyền riêng tư hay không. ID tiến trình, dòng lệnh, người dùng và đường dẫn bị loại trừ. Hoạt động không chứng minh tiến độ, hoàn tất, tình trạng treo hay lỗi. Không có thao tác cập nhật, cài đặt, truyền, tác vụ, dịch vụ, tiến trình, chính sách, khởi động lại hay sửa chữa nào được yêu cầu.

### Bản cập nhật có sẵn

Tìm các bản cập nhật phần mềm và trình điều khiển đã chọn bằng nguồn đã cấu hình, Windows Update hoặc một dịch vụ Microsoft Update đã đăng ký sẵn. Mức sẵn sàng của nguồn được kiểm tra trước khi tìm. Không cài đặt, không chấp nhận giấy phép, không đăng ký nguồn và không thay đổi chính sách. Đây là phần tách biệt với sửa chữa bảo trì.

### Windows Update

Kiểm kê có giới hạn trạng thái của gói bảo trì, tính năng tùy chọn và khả năng của Windows, kiểm tra chỉ báo khởi động lại, dung lượng trống và các dịch vụ cập nhật, quan sát Delivery Optimization mà không khởi động nó, sau đó quét kho thành phần và các tệp hệ thống được bảo vệ. Trạng thái chờ của gói, tính năng hoặc khả năng cần được xem lại nhưng không cho phép thao tác trên thành phần. Tự động sửa có thể khởi động các dịch vụ Windows Update, BITS, Dịch vụ mật mã, Trình cài đặt mô-đun Windows và Update Orchestrator đang bật. Khởi động Update Orchestrator có thể tiếp tục hoạt động cập nhật đã lên lịch hiện có theo chính sách hiện tại. Gói, tính năng tùy chọn, khả năng, bộ nhớ đệm Delivery Optimization, máy ngang hàng, băng thông, chính sách, đăng ký nguồn, lịch và cài đặt khởi động được giữ nguyên. DISM hoặc SFC cần bằng chứng trực tiếp về hỏng hóc. Quy trình này không chọn hay phê duyệt bản cập nhật nào.

### Dịch vụ Windows Update

Kiểm tra trạng thái chính xác của Dịch vụ Windows Update, DLL dịch vụ đã đăng ký của nó, hoạt động cập nhật hiện tại có giới hạn và lịch sử lỗi chính xác của dịch vụ. Quan sát DLL, hoạt động cập nhật và lỗi trong quá khứ không thể cho phép khởi động dịch vụ hay thao tác cập nhật. Khôi phục tùy chọn chỉ khởi động một dịch vụ tự động đã dừng sau khi kiểm tra mới về phụ thuộc và đăng ký tệp thực thi. Một dịch vụ khởi động theo yêu cầu đã dừng chỉ mang tính thông tin. Hoạt động cập nhật hiện có có thể tiếp tục theo chính sách hiện tại. Chính sách cập nhật, đăng ký nguồn, lịch, cài đặt khởi động lại, gói, tác vụ BITS, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn, phê duyệt hay cài đặt bản cập nhật nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khả năng truy cập nguồn, khả năng áp dụng, tải xuống, cài đặt, hoàn tất hay mức sẵn sàng khởi động lại.

### Tối ưu hóa phân phối

Kiểm tra trạng thái chính xác của dịch vụ Delivery Optimization. Một dịch vụ khởi động theo yêu cầu đã dừng chỉ mang tính thông tin. Chỉ trạng thái Paused chính xác mới có thể đề xuất khôi phục. Khôi phục tùy chọn lặp lại kiểm tra trạng thái, gửi một yêu cầu tiếp tục có giới hạn và cần hai lần quan sát Running nhất quán. Kiểu khởi động, nội dung bộ nhớ đệm, máy ngang hàng, băng thông, nguồn cập nhật và chính sách được quản lý được giữ nguyên. Hoạt động truyền, tình trạng bộ nhớ đệm, tiến độ và việc hoàn tất cập nhật vẫn chưa được xác minh.

### Dịch vụ BITS

Kiểm tra trạng thái chính xác của Background Intelligent Transfer Service, các tác vụ truyền của tài khoản hiện tại và lịch sử lỗi chính xác có giới hạn của dịch vụ. Lỗi trong quá khứ và ảnh chụp truyền không thể cho phép khởi động dịch vụ. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các ứng dụng hiện có có thể tiếp tục các lượt truyền nền trong hàng đợi. Tác vụ, mức ưu tiên, chủ sở hữu, đích, cài đặt proxy, chính sách và kiểu khởi động được giữ nguyên. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm tiến độ truyền, quyền sở hữu của ứng dụng hay việc hoàn tất.

### Truyền nền (BITS)

Đọc các tác vụ truyền BITS trong phạm vi tài khoản đã chọn và xem lại danh tính chính xác của tác vụ. Tên hàng đợi không xác lập quyền sở hữu của ứng dụng. Không có lượt truyền nào được tiếp tục, hoàn tất, hủy, xóa hay chuyển giao.

### Dịch vụ mật mã

Kiểm tra trạng thái chính xác của Dịch vụ mật mã, DLL dịch vụ đã đăng ký của nó và lịch sử lỗi chính xác có giới hạn của dịch vụ. Lỗi trong quá khứ và quan sát DLL không thể cho phép khởi động dịch vụ hay thay thế thành phần. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các thao tác danh mục, chứng chỉ và cập nhật hiện có có thể tiếp tục. Chứng chỉ, khóa, danh mục, cài đặt tin cậy, chính sách và kiểu khởi động được giữ nguyên. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc xác thực chứng chỉ, chữ ký, danh mục hay độ tin cậy.

### Trình cài đặt mô-đun Windows

Kiểm tra trạng thái chính xác của Trình cài đặt mô-đun Windows, các chuyển đổi có giới hạn của gói bảo trì và lịch sử lỗi chính xác của dịch vụ. Lỗi trong quá khứ và trạng thái gói không thể cho phép khởi động dịch vụ hay thao tác trên gói. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Hoạt động bảo trì hiện có có thể tiếp tục. Gói, nội dung kho thành phần, nguồn, chính sách và kiểu khởi động được giữ nguyên. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm tình trạng kho thành phần, khả năng áp dụng gói, việc hoàn tất bảo trì hay mức sẵn sàng khởi động lại.

### Dịch vụ Windows Update Medic

Kiểm tra trạng thái chính xác của Dịch vụ Windows Update Medic, DLL dịch vụ đã đăng ký của nó, hoạt động cập nhật hiện tại có giới hạn và lịch sử lỗi chính xác của dịch vụ. Quan sát DLL, hoạt động cập nhật và lỗi trong quá khứ không thể cho phép khởi động dịch vụ hay thao tác trên thành phần. Một dịch vụ khởi động theo yêu cầu đã dừng có thể là bình thường. Khi đúng công cụ này được chọn, khôi phục tùy chọn chỉ có thể khởi động dịch vụ đang bật đã dừng sau khi kiểm tra phụ thuộc mới. Việc khắc phục của Windows có thể tiếp tục theo chính sách hiện tại và có thể sửa các thành phần cập nhật hoặc kích hoạt hoạt động cập nhật. Chính sách cập nhật, đăng ký nguồn, lịch, gói, tác vụ BITS, cài đặt khởi động lại, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn bản cập nhật nào và không thay đổi cài đặt cập nhật nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc khắc phục, tình trạng thành phần, khả năng áp dụng, tải xuống, cài đặt hay mức sẵn sàng khởi động lại.

### Dịch vụ Update Orchestrator

Kiểm tra trạng thái chính xác của Dịch vụ Update Orchestrator, tác vụ Windows Update tích hợp chính xác, hoạt động hiện tại có giới hạn và lịch sử lỗi chính xác của dịch vụ. Kết quả tác vụ, hoạt động cập nhật và lỗi trong quá khứ không thể cho phép khởi động dịch vụ hay thao tác cập nhật. Khôi phục tùy chọn chỉ khởi động một dịch vụ tự động đã dừng sau khi kiểm tra mới về phụ thuộc và đăng ký tệp thực thi. Một dịch vụ khởi động theo yêu cầu đã dừng chỉ mang tính thông tin. Hoạt động cập nhật đã lên lịch hiện có có thể tiếp tục theo chính sách hiện tại và có thể tải xuống hoặc cài đặt các bản cập nhật đã được phê duyệt. Chính sách cập nhật, đăng ký nguồn, lịch, cài đặt khởi động lại, gói, tác vụ BITS, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn hay phê duyệt bản cập nhật nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khả năng áp dụng, tải xuống, cài đặt, hoàn tất hay mức sẵn sàng khởi động lại.

### Dọn dẹp kho thành phần

Phân tích xem Windows có đề xuất dọn dẹp kho thành phần hay không. Bảo trì tùy chọn xóa ngay các thành phần đã bị thay thế, sau đó phân tích lại. Không dùng ResetBase và không xóa thủ công WinSxS. Không có hoàn tác tự động; dung lượng đĩa thu hồi được không được đo.
