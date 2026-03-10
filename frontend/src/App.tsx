import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers/AppProviders";
import { SiteLayout } from "@/app/layout/SiteLayout";
import DemoPage from "@/pages/DemoPage";

const App = () => (
  <AppProviders>
    <BrowserRouter>
      <Routes>
        <Route element={<SiteLayout />}>
          <Route path="/" element={<DemoPage />} />
          <Route path="/demo" element={<Navigate to="/" replace />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </AppProviders>
);

export default App;
