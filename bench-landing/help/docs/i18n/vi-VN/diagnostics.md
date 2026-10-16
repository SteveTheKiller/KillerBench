# Chẩn đoán

Chẩn đoán là nơi mọi lần xử lý nên bắt đầu. Mỗi công cụ đọc máy tính và viết một báo cáo. Không có gì ở đây thay đổi Windows. Công cụ mặc định, quét toàn bộ hệ thống, chạy một loạt rộng các kiểm tra chỉ đọc và xếp hạng những gì cần chú ý.

Hướng dẫn từng bước: [Điều tra sự cố treo máy và màn hình xanh](investigate-crashes.html) và [Sửa tệp hệ thống](repair-system-files.html).

## Công cụ trong mục này

Kiểm tra máy tính trước, sau đó đọc báo cáo để biết những gì cần chú ý và bước an toàn tiếp theo.

### Quét toàn bộ hệ thống

Kiểm tra 49 vùng chẩn đoán, sau đó đánh giá mọi công thức khôi phục dịch vụ đã cài cùng với các công thức bảo trì, DHCP, in ấn, Defender, thời gian và WMI. Các kiểm tra có thể mất vài phút. Tự động sửa là tùy chọn và cần bằng chứng hiện tại riêng của từng công thức, điều kiện tiên quyết của máy và kiểm tra phụ thuộc. Các sửa chữa đủ điều kiện có thể gia hạn thuê DHCP bị thiếu, giải phóng tác vụ in trong hàng đợi, đồng bộ lại đồng hồ, làm mới chữ ký Defender, khôi phục các dịch vụ đủ điều kiện và sửa hỏng hóc Windows đã được xác nhận. Các dịch vụ tự động đã dừng có thể được khởi động sau khi khoảng thời gian khởi động và các phụ thuộc của chúng được xác minh. Một dịch vụ khởi động theo yêu cầu đã dừng được giữ nguyên khi trạng thái nhàn rỗi là bằng chứng duy nhất; các quy trình chuyên biệt hẹp chỉ có thể yêu cầu nó khi bằng chứng hiện tại về tính năng hỗ trợ việc khởi động. Kiểu khởi động, định nghĩa tác vụ, báo cáo, tệp kết xuất, đăng ký nhận, thông tin xác thực, cài đặt kênh, chính sách tường lửa và cài đặt được quản lý được giữ nguyên. Ảnh chụp lịch sử, sao lưu và truyền chỉ đọc. Nội dung tệp kết xuất không được mở. Việc kiểm tra bit bẩn không lên lịch sửa chữa hay khởi động lại. Quy trình này không cài bản cập nhật, không thay đổi trình điều khiển, không xóa tệp và không thực hiện kiểm tra phần cứng toàn diện.

### Chỉ số chính

Ghi lại bộ nhớ khả dụng, tình trạng đĩa do nhà cung cấp báo cáo và trạng thái bảo vệ BitLocker, sau đó đối chiếu các chỉ số hiện tại với sự cố thiết bị, lịch sử độ tin cậy, bản ghi lỗi phần cứng, bằng chứng sự cố, các lần tắt máy bất ngờ và kết quả Windows Memory Diagnostic. Chỉ báo khởi động lại, dung lượng trống của đĩa hệ thống, việc nâng quyền và cấu hình dịch vụ cập nhật vẫn hiển thị. Mật khẩu khôi phục không được thu thập. Không có sửa chữa nào chạy trong quy trình này. Kết quả tổng hợp không phải là một bài kiểm tra toàn diện về phần cứng, phần mềm hay độ ổn định.

### Lịch sử độ tin cậy

Đọc các bản ghi Windows Reliability Monitor trong 14 ngày gần nhất cùng với các lỗi Application gần đây và sự kiện tắt máy bất ngờ, sau đó trả về một kết luận lịch sử có giới hạn. Bản ghi trong quá khứ không xác lập tình trạng hiện tại hay nguyên nhân gốc. Không có sửa chữa nào được chọn và không có ứng dụng, dịch vụ hay cài đặt nào bị thay đổi.

### Hiệu suất khởi động

Đọc siêu dữ liệu sự kiện hiệu suất khởi động và tắt máy của Windows trong 14 ngày gần nhất, chỉ giữ danh tính nhà cung cấp và các trường thời lượng dạng số có giới hạn, sau đó đối chiếu với các chỉ báo khởi động lại đang chờ được hỗ trợ. Đường dẫn, tên tiến trình, tài khoản và nội dung sự kiện khác được loại bỏ trước khi lưu bằng chứng. Bản ghi suy giảm trong quá khứ không xác định nút thắt hiện tại và không cho phép thay đổi khởi động, dịch vụ, ứng dụng, trình điều khiển, tác vụ, nguồn điện, khởi động lại hay chính sách.

### Lịch sử lỗi phần cứng

Đọc lịch sử Windows Hardware Error Architecture và Memory Diagnostic trong 14 ngày gần nhất, các sự cố thiết bị Plug and Play hiện tại, tình trạng đĩa vật lý và bộ đếm độ tin cậy của nhà cung cấp, sau đó trả về một kết luận phần cứng có giới hạn. Các bản ghi cần được đối chiếu với nội dung của chúng, chẩn đoán phần sụn và kiểm tra phần cứng của nhà cung cấp. Không suy ra thành phần hỏng hay nguyên nhân gốc, không chọn sửa chữa và không thay đổi phần cứng, phần sụn, trình điều khiển, đĩa, nhóm, ổ đĩa, tệp, dịch vụ, nguồn điện, khởi động lại hay cài đặt nào.

### Lịch sử cài đặt trình điều khiển

Đọc phần cuối được giữ lại của lịch sử cài đặt thiết bị SetupAPI của Windows, đối chiếu các phần thất bại với danh tính Plug and Play hiện tại chính xác và trả về một kết luận có giới hạn. Các phần nhật ký trong quá khứ không chứng minh thiết bị vẫn còn hay lỗi vẫn đang xảy ra. Không có trình điều khiển nào được cài, gỡ, khôi phục phiên bản cũ hay cập nhật.

### Điểm Khôi phục Hệ thống

Liệt kê tối đa 256 điểm Khôi phục Hệ thống khả dụng, xem lại điều kiện tạo điểm tường minh và trả về một kết luận có giới hạn về mức sẵn sàng. Danh sách không chứng minh rằng bảo vệ đã được bật cho mọi ổ đĩa hay việc khôi phục sẽ thành công. Không có điểm khôi phục nào được tạo, xóa hay áp dụng trong khi chẩn đoán.

### Môi trường khôi phục Windows

Đọc cấu hình Môi trường khôi phục Windows hiện tại bằng REAgentC, đối chiếu vị trí cục bộ đang bật với đúng đĩa và phân vùng hiện tại, và trả về một kết luận có giới hạn về mức sẵn sàng. Sửa chữa tự động tùy chọn có thể bật một cấu hình hiện có đang bị tắt, sau đó xác minh trạng thái được báo cáo. Ảnh khôi phục không được tải xuống, di chuyển hay thay thế, không có lần khởi động vào chế độ khôi phục nào được kích hoạt và Windows không được khởi động lại. Trạng thái đã bật và sự hiện diện của phân vùng không chứng minh rằng ảnh khôi phục có thể khởi động hay sửa chữa PC này.

### Kích hoạt Windows

Đọc trạng thái giấy phép Windows hiện tại mà không thu thập khóa sản phẩm đầy đủ hay một phần, giữ lại siêu dữ liệu đã làm sạch của các sự kiện cảnh báo, lỗi và nghiêm trọng của Software Protection trong bảy ngày gần nhất, sau đó trả về một kết luận hoạt động có giới hạn. Nội dung sự kiện được loại bỏ trước khi lưu bằng chứng. Bản ghi trong quá khứ chỉ mang tính ngữ cảnh khi trạng thái cấp phép hiện tại báo Licensed. Không có thao tác nào về kích hoạt, khóa sản phẩm, mã thông báo, giấy phép, tài khoản, dịch vụ hay chính sách được chọn. Trạng thái nhà cung cấp và lịch sử sự kiện không chứng minh quyền thuê bao, khả năng truy cập máy chủ kích hoạt, tính toàn vẹn của kho mã thông báo, hiệu lực trong tương lai hay nguyên nhân gốc.

### Dịch vụ Windows

Kiểm kê tên dịch vụ đã cài, tên hiển thị, trạng thái, chế độ khởi động và loại hành động khôi phục có giới hạn về quyền riêng tư mà không thu thập tài khoản, đường dẫn tệp thực thi, lệnh khôi phục hay thông báo khởi động lại. Xem lại tính nhất quán của chế độ khởi động, danh tính và chu trình của phụ thuộc trực tiếp, lặp lại các trạng thái chờ chính xác, kiểm tra các dịch vụ được hỗ trợ và phụ thuộc của các dịch vụ đã dừng, sau đó chỉ đối chiếu các lỗi gần đây của Service Control Manager khi có đúng một tên dịch vụ chính xác hoặc tên hiển thị duy nhất. Việc dùng nhóm thứ tự tải được đếm mà không mở rộng thành viên, và các danh tính mơ hồ vẫn chưa được giải quyết. Các dịch vụ khởi động theo yêu cầu đã dừng có thể là bình thường. Không có dịch vụ nào được khởi động, và cài đặt khởi động, phụ thuộc và khôi phục được giữ nguyên. Dùng quy trình Sửa chữa tương ứng khi một tính năng cụ thể cần khôi phục.

### Lỗi ứng dụng

Đọc các sự kiện lỗi và nghiêm trọng gần đây của Application cùng với lịch sử Reliability Monitor có giới hạn, sau đó trả về một kết luận về lịch sử độ ổn định. Bản ghi trong quá khứ không chứng minh lỗi hiện tại hay nguyên nhân gốc và chỉ hướng tới chẩn đoán hẹp hơn. Không có ứng dụng nào bị dừng, sửa, gỡ bỏ hay cấu hình lại.

### Thiết bị hiển thị

Kiểm tra bộ điều hợp đồ họa và màn hình để tìm lỗi thiết bị được báo cáo, ID phần cứng và phiên bản trình điều khiển đã cài. Không cài trình điều khiển, không đặt lại thiết bị và không thay đổi độ phân giải. Chất lượng hình ảnh và kết nối vật lý không được kiểm tra.

### Thiết bị nhập

Kiểm tra lỗi của bàn phím, chuột và thiết bị HID cùng danh sách trình điều khiển. Không ghi lại thao tác gõ phím, không đặt lại thiết bị và không thay đổi trình điều khiển. Hoạt động nhập vật lý không được kiểm tra.

### Sự cố thiết bị

Tìm các mã lỗi cấu hình Plug and Play khác không trên mọi lớp thiết bị, sau đó đối chiếu bằng chứng về thiết bị hiện có với lịch sử SetupAPI có giới hạn về cài đặt trình điều khiển, thiết lập thiết bị và Plug and Play. Chỉ riêng bản ghi trong quá khứ không xác lập lỗi hiện tại. Xem lại danh tính thiết bị chính xác và bằng chứng về trình điều khiển đã được phê duyệt mà không thay đổi phần cứng, trình điều khiển hay cấu hình. Thiết bị bị thiếu và hoạt động vật lý không được xác minh.
