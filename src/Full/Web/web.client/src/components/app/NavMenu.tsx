import { useHistory } from 'react-router-dom';
import { TabValue, TabList, Tab, SelectTabData, SelectTabEvent } from '@fluentui/react-components';
import { useState } from 'react';

export function NavMenu() {

  const [selectedValue, setSelectedValue] = useState<TabValue>("home");
  const history = useHistory();
  const onTabSelect = (_: SelectTabEvent, data: SelectTabData) => {
    setSelectedValue(data.value);
    if (data.value === "home") {
      history.push('/tabhome');
    }
  };

  return (
    <div className='nav'>
      <TabList selectedValue={selectedValue} onTabSelect={onTabSelect}>
        <Tab id="Home" value="home">
          Home
        </Tab>
      </TabList>
    </div>
  );

}
