import userEvent from "@testing-library/user-event"

// delay: null 省掉每個動作之間的 setTimeout，負載高時才不會逾時
export const user = userEvent.setup({ delay: null })
