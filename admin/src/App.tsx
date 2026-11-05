import { ThemedLayout, ThemedSider, ThemedTitle, useNotificationProvider } from "@refinedev/antd"
import { Refine } from "@refinedev/core"
import routerProvider, { NavigateToResource } from "@refinedev/react-router"
import { BarChartOutlined } from "@ant-design/icons"
import { App as AntdApp, ConfigProvider } from "antd"
import zhTW from "antd/locale/zh_TW"
import { Outlet, Route, Routes } from "react-router"
import { dataProvider } from "./api/dataProvider"
import { ContentCreate, ContentEdit, ContentList } from "./content/ContentPages"
import { contentResources } from "./content/resources"
import { DraftError } from "./draft/DraftBar"
import { AdminHeader } from "./layout/AdminHeader"
import { ReportPage } from "./report/ReportPage"
import "@refinedev/antd/dist/reset.css"

export const AppTitle = "EBO 內容後台"

export function App() {
  return (
    <ConfigProvider locale={zhTW} button={{ autoInsertSpace: false }}>
      <AntdApp>
        <Refine
          dataProvider={dataProvider}
          routerProvider={routerProvider}
          notificationProvider={useNotificationProvider}
          options={{ disableTelemetry: true, mutationMode: "pessimistic" }}
          resources={[
            ...contentResources.map((resource) => ({
              name: resource.name,
              list: `/${resource.name}`,
              create: `/${resource.name}/create`,
              edit: `/${resource.name}/edit/:id`,
              meta: { label: resource.label, icon: <resource.icon /> },
            })),
            {
              name: "reports",
              list: "/reports",
              meta: { label: "平衡報告", icon: <BarChartOutlined /> },
            },
          ]}
        >
          <Routes>
            <Route
              element={
                <ThemedLayout
                  Header={AdminHeader}
                  Sider={(props) => <ThemedSider {...props} fixed />}
                  Title={(props) => <ThemedTitle {...props} text={AppTitle} />}
                >
                  <DraftError />
                  <Outlet />
                </ThemedLayout>
              }
            >
              <Route index element={<NavigateToResource resource={contentResources[0].name} />} />
              <Route path="reports" element={<ReportPage />} />
              {contentResources.map((resource) => (
                <Route key={resource.name} path={resource.name}>
                  <Route index element={<ContentList resource={resource} />} />
                  <Route path="create" element={<ContentCreate resource={resource} />} />
                  <Route path="edit/:id" element={<ContentEdit resource={resource} />} />
                </Route>
              ))}
            </Route>
          </Routes>
        </Refine>
      </AntdApp>
    </ConfigProvider>
  )
}
