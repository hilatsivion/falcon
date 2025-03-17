import React from "react";
import { Routes, Route } from "react-router-dom";
import OnboardingRoutes from "./OnboardingRoutes";
import MainRoutes from "./MainRoutes";

const AppRoutes = () => {
  return (
    <Routes>
      {/* אחרי פיתוח המסכים הראשיים צריך לשנות את הניתוב כך שזה יעבוד לפי לוקל סטורג שמגיע מהתוקן */}
      {/* <Route path="/*" element={<OnboardingRoutes />} /> */}
      <Route path="/*" element={<MainRoutes />} />
    </Routes>
  );
};

export default AppRoutes;
