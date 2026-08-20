using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MommyAndSunny.API.Filters
{
    // Chạy TRƯỚC mọi action: với mỗi tham số action (VD: [FromBody] request), tự tìm
    // IValidator<T> tương ứng đã đăng ký trong DI (Scrutor/AddValidatorsFromAssemblyContaining
    // chỉ ĐĂNG KÝ validator, KHÔNG tự gọi nó — filter này là mắt xích còn thiếu để validator
    // thực sự chạy mà KHÔNG cần inject + gọi ValidateAsync thủ công trong từng controller).
    // Action nào không có tham số khớp IValidator<T> nào thì filter bỏ qua, không ảnh hưởng.
    public class ValidationActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                {
                    continue;
                }

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        data = (object?)null,
                        message = "Dữ liệu không hợp lệ",
                        errors = result.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                    });
                    return;
                }
            }

            await next();
        }
    }
}
